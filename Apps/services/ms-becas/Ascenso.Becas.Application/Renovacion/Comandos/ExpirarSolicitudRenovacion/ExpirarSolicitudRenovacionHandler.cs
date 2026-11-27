using Ascenso.Becas.Application.Comun;
using Ascenso.Becas.Application.Despacho;
using Ascenso.Becas.Application.Excepciones;
using Ascenso.Becas.Application.Otorgamiento;
using Ascenso.Becas.Application.Otorgamiento.Puertos;
using Ascenso.Becas.Application.Renovacion.Puertos;
using Ascenso.Becas.Domain.Enums;
using Ascenso.Becas.Domain.Ports;

namespace Ascenso.Becas.Application.Renovacion.Comandos.ExpirarSolicitudRenovacion;

/// <summary>Concluye una solicitud existente que venció sin decisión.</summary>
public sealed class ExpirarSolicitudRenovacionHandler
    : ICommandHandler<ExpirarSolicitudRenovacionCommand, bool>
{
    private readonly ISolicitudRenovacionRepository _solicitudes;
    private readonly IBecaRepository _becas;
    private readonly IVentanaRenovacionPrograma _ventanas;
    private readonly ILiberacionCupoPrograma _cupos;
    private readonly IProgramasClient _programas;
    private readonly IUnitOfWork _unidad;
    private readonly IContextoOperacionActual _contextoActual;
    private readonly IClock _reloj;
    private readonly IOutboxBeca _outbox;

    public ExpirarSolicitudRenovacionHandler(
        ISolicitudRenovacionRepository solicitudes, IBecaRepository becas,
        IVentanaRenovacionPrograma ventanas, ILiberacionCupoPrograma cupos,
        IProgramasClient programas, IUnitOfWork unidad,
        IContextoOperacionActual contextoActual, IClock reloj, IOutboxBeca outbox)
    {
        _solicitudes = solicitudes ?? throw new ArgumentNullException(nameof(solicitudes));
        _becas = becas ?? throw new ArgumentNullException(nameof(becas));
        _ventanas = ventanas ?? throw new ArgumentNullException(nameof(ventanas));
        _cupos = cupos ?? throw new ArgumentNullException(nameof(cupos));
        _programas = programas ?? throw new ArgumentNullException(nameof(programas));
        _unidad = unidad ?? throw new ArgumentNullException(nameof(unidad));
        _contextoActual = contextoActual ?? throw new ArgumentNullException(nameof(contextoActual));
        _reloj = reloj ?? throw new ArgumentNullException(nameof(reloj));
        _outbox = outbox ?? throw new ArgumentNullException(nameof(outbox));
    }

    public async Task<bool> HandleAsync(ExpirarSolicitudRenovacionCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (command.SolicitudId == Guid.Empty)
            throw new SolicitudInvalidaException(
                "La solicitud de renovación es obligatoria.");

        var referencia = await _solicitudes.ObtenerPorIdAsync(
            command.SolicitudId, cancellationToken);
        if (referencia is null || referencia.Estado != EstadoRenovacion.SolicitadaEnCurso)
            return false;
        var hoy = DateOnly.FromDateTime(_reloj.UtcNow.UtcDateTime);
        if (hoy <= referencia.FechaLimiteVentanaRenovacion)
            return false;
        var becaReferencia = await _becas.ObtenerPorIdAsync(
            referencia.BecaId, cancellationToken)
            ?? throw new RecursoNoEncontradoException("beca", referencia.BecaId);
        var ventana = await _ventanas.ObtenerAsync(
            becaReferencia.ProgramaBecaId, referencia.PeriodoSolicitadoId,
            cancellationToken);
        if (ventana is null || !ventana.Activo || ventana.Inicio == default || ventana.Fin == default
            || ventana.Fin < ventana.Inicio
            || ventana.PermiteRenovacionTardiaExcepcional is null)
            throw new ConfiguracionProgramaNoDisponibleException(
                becaReferencia.ProgramaBecaId,
                "No están confirmadas la ventana y la política de renovación tardía.");
        if (ventana.PermiteRenovacionTardiaExcepcional.Value
            || hoy <= ventana.Fin)
            return false;

        var expirada = false;
        await _unidad.EjecutarEnTransaccionAsync(async token =>
        {
            var solicitud = await _solicitudes.ObtenerParaActualizarAsync(
                command.SolicitudId, token);
            if (solicitud is null
                || solicitud.Estado != EstadoRenovacion.SolicitadaEnCurso
                || DateOnly.FromDateTime(_reloj.UtcNow.UtcDateTime)
                    <= solicitud.FechaLimiteVentanaRenovacion)
                return;
            var beca = await _becas.ObtenerBecaParaActualizarAsync(
                solicitud.BecaId, token)
                ?? throw new RecursoNoEncontradoException("beca", solicitud.BecaId);
            if (beca.Estado != EstadoBeca.Activa || !beca.TieneRenovacionEnCurso
                || beca.PeriodoVigenteActualId != solicitud.PeriodoOrigenId)
                throw new ConflictoOperacionException(
                    "La beca no coincide con la renovación pendiente de vencimiento.");

            if (solicitud.ReservaPresupuestoId is Guid reservaId)
                await _programas.LiberarReservaAsync(beca.ProgramaBecaId,
                    reservaId, solicitud.Id, token);
            var cupoLiberado = await _cupos.LiberarPorConclusionAsync(
                beca.ProgramaBecaId, beca.Id, beca.PeriodoVigenteActualId,
                token);
            if (cupoLiberado is not true)
                throw new DependenciaNoDisponibleException("MS-PRG",
                    new InvalidOperationException(
                        "MS-PRG no confirmó la liberación del cupo ocupado."));

            var contexto = ContextoAplicacion.Crear(_contextoActual, _reloj,
                "Venció la ventana de renovación sin decisión.",
                exigirUsuario: false);
            solicitud.Expirar(contexto);
            beca.ConcluirPorVencimientoRenovacion(
                solicitud.PeriodoSolicitadoId, contexto);
            foreach (var evento in beca.EventosDominio)
                await _outbox.EncolarAsync(evento, beca.Version, token);
            await _unidad.GuardarCambiosAsync(token);
            expirada = true;
        }, NivelAislamientoTransaccion.Serializable, cancellationToken);
        return expirada;
    }
}
