using Ascenso.Becas.Application.Comun;
using Ascenso.Becas.Application.Despacho;
using Ascenso.Becas.Application.Excepciones;
using Ascenso.Becas.Application.Otorgamiento;
using Ascenso.Becas.Application.Renovacion.Dtos;
using Ascenso.Becas.Domain.Enums;
using Ascenso.Becas.Domain.Ports;

namespace Ascenso.Becas.Application.Renovacion.Comandos.EvaluarRenovacion;

/// <summary>
/// Registra el rechazo sin reserva; aprobación y liberación de reservas esperan
/// la coordinación financiera y la creación atómica de la beca sucesora.
/// </summary>
public sealed class EvaluarRenovacionHandler
    : ICommandHandler<EvaluarRenovacionCommand, SolicitudRenovacionDto>
{
    private readonly ISolicitudRenovacionRepository _solicitudes;
    private readonly IBecaRepository _becas;
    private readonly IUnitOfWork _unidad;
    private readonly IContextoOperacionActual _contextoActual;
    private readonly IClock _reloj;

    public EvaluarRenovacionHandler(ISolicitudRenovacionRepository solicitudes,
        IBecaRepository becas, IUnitOfWork unidad,
        IContextoOperacionActual contextoActual, IClock reloj)
    {
        _solicitudes = solicitudes ?? throw new ArgumentNullException(nameof(solicitudes));
        _becas = becas ?? throw new ArgumentNullException(nameof(becas));
        _unidad = unidad ?? throw new ArgumentNullException(nameof(unidad));
        _contextoActual = contextoActual ?? throw new ArgumentNullException(nameof(contextoActual));
        _reloj = reloj ?? throw new ArgumentNullException(nameof(reloj));
    }

    public async Task<SolicitudRenovacionDto> HandleAsync(
        EvaluarRenovacionCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (command.SolicitudRenovacionId == Guid.Empty
            || string.IsNullOrWhiteSpace(command.MotivoDecision))
            throw new SolicitudInvalidaException(
                "La solicitud y el motivo de evaluación son obligatorios.");
        if (command.Aprobar)
            throw new ConflictoOperacionException(
                "La aprobación requiere reserva confirmada y creación de la beca sucesora en la misma transacción.");

        SolicitudRenovacionDto? resultado = null;
        await _unidad.EjecutarEnTransaccionAsync(async token =>
        {
            var solicitud = await _solicitudes.ObtenerParaActualizarAsync(
                command.SolicitudRenovacionId, token)
                ?? throw new RecursoNoEncontradoException(
                    "solicitud de renovación", command.SolicitudRenovacionId);
            if (solicitud.Estado != EstadoRenovacion.SolicitadaEnCurso)
                throw new ConflictoOperacionException(
                    "La solicitud de renovación ya tiene una decisión.");
            if (solicitud.ReservaPresupuestoId is not null)
                throw new ConflictoOperacionException(
                    "Debe liberarse la reserva en MS-PRG antes de rechazar la renovación.");
            var beca = await _becas.ObtenerBecaParaActualizarAsync(
                solicitud.BecaId, token)
                ?? throw new RecursoNoEncontradoException("beca", solicitud.BecaId);

            var contexto = ContextoAplicacion.Crear(_contextoActual, _reloj,
                command.MotivoDecision);
            try
            {
                solicitud.Rechazar(command.MotivoDecision, contexto);
                beca.RechazarRenovacionEnCurso(contexto);
            }
            catch (InvalidOperationException excepcion)
            {
                throw new ConflictoOperacionException(excepcion.Message);
            }

            await _unidad.GuardarCambiosAsync(token);
            resultado = SolicitudRenovacionDto.Desde(solicitud);
        }, NivelAislamientoTransaccion.Serializable, cancellationToken);

        return resultado ?? throw new InvalidOperationException(
            "La evaluación de renovación no produjo un resultado.");
    }
}
