using Ascenso.Becas.Application.Despacho;
using Ascenso.Becas.Application.Excepciones;
using Ascenso.Becas.Application.Renovacion.Comandos.ConcluirBecaPorVencimiento;
using Ascenso.Becas.Application.Renovacion.Puertos;
using Ascenso.Becas.Domain.Ports;
using Ascenso.Becas.Infrastructure.Dependencias;
using Ascenso.Becas.Infrastructure.Observabilidad;
using Hangfire;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Ascenso.Becas.Infrastructure.Trabajos;

/// <summary>Descubre becas sin solicitud al vencer la ventana oficial siguiente.</summary>
public sealed class DescubrirConclusionesRenovacionJob
{
    private static readonly Action<ILogger, Guid, Exception?> _calendarioPendiente =
        LoggerMessage.Define<Guid>(LogLevel.Warning,
            new EventId(1, nameof(DescubrirConclusionesRenovacionJob)),
            "No se pudo verificar el calendario de renovación del programa {ProgramaId}.");
    private static readonly Action<ILogger, Guid, Exception?> _conclusionPendiente =
        LoggerMessage.Define<Guid>(LogLevel.Warning,
            new EventId(2, nameof(DescubrirConclusionesRenovacionJob)),
            "La conclusión de la beca {BecaId} permanece pendiente.");

    private readonly IBecaRepository _becas;
    private readonly IPeriodoSiguienteRenovacionPrograma _periodos;
    private readonly ICommandDispatcher _despachador;
    private readonly IClock _reloj;
    private readonly IOptions<OpcionesTrabajosHangfire> _opciones;
    private readonly ILogger<DescubrirConclusionesRenovacionJob> _logger;

    public DescubrirConclusionesRenovacionJob(IBecaRepository becas,
        IPeriodoSiguienteRenovacionPrograma periodos,
        ICommandDispatcher despachador, IClock reloj,
        IOptions<OpcionesTrabajosHangfire> opciones,
        ILogger<DescubrirConclusionesRenovacionJob> logger)
    {
        _becas = becas ?? throw new ArgumentNullException(nameof(becas));
        _periodos = periodos ?? throw new ArgumentNullException(nameof(periodos));
        _despachador = despachador ?? throw new ArgumentNullException(nameof(despachador));
        _reloj = reloj ?? throw new ArgumentNullException(nameof(reloj));
        _opciones = opciones ?? throw new ArgumentNullException(nameof(opciones));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [Queue(RegistroHangfire.ColaCritica)]
    public Task Ejecutar(CancellationToken cancellationToken = default)
        => TelemetriaHangfire.EjecutarAsync(
            nameof(DescubrirConclusionesRenovacionJob),
            () => EjecutarAsync(cancellationToken));

    private async Task EjecutarAsync(CancellationToken cancellationToken)
    {
        var hoy = DateOnly.FromDateTime(_reloj.UtcNow.UtcDateTime);
        var limite = Math.Max(1, _opciones.Value.LimiteRenovacionesPorReconciliacion);
        var periodos = new Dictionary<(Guid ProgramaId, Guid PeriodoOrigenId),
            PeriodoSiguienteRenovacionPrograma?>();
        Guid? ultimoId = null;
        while (true)
        {
            var candidatas = await _becas.ObtenerActivasSinRenovacionAsync(
                limite, ultimoId, cancellationToken);
            if (candidatas.Count == 0)
                break;
            foreach (var beca in candidatas)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var clave = (ProgramaId: beca.ProgramaBecaId,
                    PeriodoOrigenId: beca.PeriodoOrigenId);
                if (!periodos.TryGetValue(clave, out var siguiente))
                {
                    try
                    {
                        siguiente = await _periodos.ObtenerAsync(
                            clave.ProgramaId, clave.PeriodoOrigenId,
                            cancellationToken);
                    }
                    catch (ConfiguracionProgramaNoDisponibleException excepcion)
                    {
                        _calendarioPendiente(_logger, clave.ProgramaId, excepcion);
                    }
                    catch (HttpRequestException excepcion)
                    {
                        _calendarioPendiente(_logger, clave.ProgramaId, excepcion);
                    }
                    periodos.Add(clave, siguiente);
                }
                if (!EsVentanaVencidaValida(siguiente, beca, hoy))
                    continue;

                try
                {
                    await _despachador.EnviarAsync<ConcluirBecaPorVencimientoCommand, bool>(
                        new ConcluirBecaPorVencimientoCommand(
                            beca.BecaId, siguiente!.PeriodoSiguienteId),
                        cancellationToken);
                }
                catch (ConfiguracionProgramaNoDisponibleException excepcion)
                {
                    _conclusionPendiente(_logger, beca.BecaId, excepcion);
                }
                catch (DependenciaNoDisponibleException excepcion)
                {
                    _conclusionPendiente(_logger, beca.BecaId, excepcion);
                }
                catch (HttpRequestException excepcion)
                {
                    _conclusionPendiente(_logger, beca.BecaId, excepcion);
                }
            }
            ultimoId = candidatas[^1].BecaId;
            if (candidatas.Count < limite)
                break;
        }
    }

    private static bool EsVentanaVencidaValida(
        PeriodoSiguienteRenovacionPrograma? siguiente,
        BecaCandidataConclusion beca, DateOnly hoy)
        => siguiente is not null
            && siguiente.ProgramaBecaId == beca.ProgramaBecaId
            && siguiente.PeriodoOrigenId == beca.PeriodoOrigenId
            && siguiente.PeriodoSiguienteId != Guid.Empty
            && siguiente.PeriodoSiguienteId != beca.PeriodoOrigenId
            && siguiente.Ventana is
            {
                Activo: true,
                PermiteRenovacionTardiaExcepcional: false
            }
            && siguiente.Ventana.Inicio != default
            && siguiente.Ventana.Fin >= siguiente.Ventana.Inicio
            && hoy > siguiente.Ventana.Fin;
}
