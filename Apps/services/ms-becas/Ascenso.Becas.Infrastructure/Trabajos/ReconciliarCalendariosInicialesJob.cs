using Ascenso.Becas.Application.Desembolsos.GenerarCalendarioInicial;
using Ascenso.Becas.Application.Despacho;
using Ascenso.Becas.Application.Excepciones;
using Ascenso.Becas.Domain.Enums;
using Ascenso.Becas.Infrastructure.Dependencias;
using Ascenso.Becas.Infrastructure.Observabilidad;
using Ascenso.Becas.Infrastructure.Persistence;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Ascenso.Becas.Infrastructure.Trabajos;

/// <summary>Repara calendarios faltantes sin bloquear otras becas del lote.</summary>
public sealed class ReconciliarCalendariosInicialesJob
{
    private const int TamanoPagina = 500;
    private static readonly Action<ILogger, Guid, Exception?> _planPendiente =
        LoggerMessage.Define<Guid>(LogLevel.Warning,
            new EventId(1, nameof(ReconciliarCalendariosInicialesJob)),
            "Calendario de la beca {BecaId} pendiente por plan oficial.");
    private readonly BecasDbContext _contexto;
    private readonly ICommandDispatcher _despachador;
    private readonly ILogger<ReconciliarCalendariosInicialesJob> _logger;

    public ReconciliarCalendariosInicialesJob(BecasDbContext contexto,
        ICommandDispatcher despachador,
        ILogger<ReconciliarCalendariosInicialesJob> logger)
    {
        _contexto = contexto ?? throw new ArgumentNullException(nameof(contexto));
        _despachador = despachador ?? throw new ArgumentNullException(nameof(despachador));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [Queue(RegistroHangfire.ColaCritica)]
    public Task Ejecutar(CancellationToken cancellationToken = default)
        => TelemetriaHangfire.EjecutarAsync(
            nameof(ReconciliarCalendariosInicialesJob),
            () => EjecutarAsync(cancellationToken));

    private async Task EjecutarAsync(CancellationToken cancellationToken)
    {
        Guid? ultimoId = null;
        while (true)
        {
            var consulta = _contexto.Becas.AsNoTracking()
                .Where(beca => beca.Estado == EstadoBeca.Activa
                    && !_contexto.CalendariosDesembolso.Any(calendario =>
                        calendario.BecaId == beca.Id
                        && calendario.PeriodoAcademicoId == beca.PeriodoVigenteActualId));
            if (ultimoId.HasValue)
                consulta = consulta.Where(beca => beca.Id.CompareTo(ultimoId.Value) > 0);
            var pendientes = await consulta.OrderBy(beca => beca.Id)
                .Take(TamanoPagina).Select(beca => beca.Id)
                .ToArrayAsync(cancellationToken);
            if (pendientes.Length == 0)
                break;

            foreach (var becaId in pendientes)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    await _despachador.EnviarAsync<GenerarCalendarioInicialCommand, bool>(
                        new GenerarCalendarioInicialCommand(becaId), cancellationToken);
                }
                catch (ConfiguracionProgramaNoDisponibleException excepcion)
                {
                    _planPendiente(_logger, becaId, excepcion);
                }
                catch (DependenciaNoDisponibleException excepcion)
                {
                    _planPendiente(_logger, becaId, excepcion);
                }
                catch (HttpRequestException excepcion)
                {
                    _planPendiente(_logger, becaId, excepcion);
                }
            }

            ultimoId = pendientes[^1];
            if (pendientes.Length < TamanoPagina)
                break;
        }
    }
}
