using Ascenso.Becas.Application.Comun;
using Ascenso.Becas.Application.Despacho;
using Ascenso.Becas.Application.Excepciones;
using Ascenso.Becas.Application.Otorgamiento;
using Ascenso.Becas.Application.Otorgamiento.Puertos;
using Ascenso.Becas.Application.Renovacion.Dtos;
using Ascenso.Becas.Application.Renovacion.Servicios;
using Ascenso.Becas.Domain.Enums;
using Ascenso.Becas.Domain.Ports;
using Ascenso.Becas.Domain.Renovacion.Agregados;

namespace Ascenso.Becas.Application.Renovacion.Comandos.SolicitarRenovacion;

/// <summary>Persiste la solicitud apta junto con la marca de la beca.</summary>
public sealed class SolicitarRenovacionHandler
    : ICommandHandler<SolicitarRenovacionCommand, SolicitudRenovacionDto>
{
    private readonly EvaluadorElegibilidadRenovacion _elegibilidad;
    private readonly IBecaRepository _becas;
    private readonly ISolicitudRenovacionRepository _solicitudes;
    private readonly IUnitOfWork _unidad;
    private readonly IContextoOperacionActual _contextoActual;
    private readonly IClock _reloj;
    private readonly IOutboxBeca _outbox;

    public SolicitarRenovacionHandler(EvaluadorElegibilidadRenovacion elegibilidad,
        IBecaRepository becas, ISolicitudRenovacionRepository solicitudes,
        IUnitOfWork unidad, IContextoOperacionActual contextoActual, IClock reloj,
        IOutboxBeca outbox)
    {
        _elegibilidad = elegibilidad ?? throw new ArgumentNullException(nameof(elegibilidad));
        _becas = becas ?? throw new ArgumentNullException(nameof(becas));
        _solicitudes = solicitudes ?? throw new ArgumentNullException(nameof(solicitudes));
        _unidad = unidad ?? throw new ArgumentNullException(nameof(unidad));
        _contextoActual = contextoActual ?? throw new ArgumentNullException(nameof(contextoActual));
        _reloj = reloj ?? throw new ArgumentNullException(nameof(reloj));
        _outbox = outbox ?? throw new ArgumentNullException(nameof(outbox));
    }

    public async Task<SolicitudRenovacionDto> HandleAsync(
        SolicitarRenovacionCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        var datos = await _elegibilidad.EvaluarAsync(command.BecaId,
            command.PeriodoSolicitadoId, command.MontoSolicitado, cancellationToken);
        SolicitudRenovacionDto? resultado = null;
        await _unidad.EjecutarEnTransaccionAsync(async token =>
        {
            var beca = await _becas.ObtenerBecaParaActualizarAsync(
                command.BecaId, token)
                ?? throw new RecursoNoEncontradoException("beca", command.BecaId);
            if (beca.Estado != EstadoBeca.Activa || beca.TieneRenovacionEnCurso
                || beca.PeriodoVigenteActualId != datos.PeriodoOrigenId
                || await _solicitudes.ExistePorBecaYPeriodoAsync(
                    beca.Id, command.PeriodoSolicitadoId, token))
                throw new ConflictoOperacionException(
                    "La beca ya no admite esta solicitud de renovación.");

            var contexto = ContextoAplicacion.Crear(_contextoActual, _reloj);
            SolicitudRenovacion solicitud;
            try
            {
                solicitud = SolicitudRenovacion.Solicitar(datos, contexto);
                beca.IniciarRenovacion(contexto);
            }
            catch (InvalidOperationException excepcion)
            {
                throw new ConflictoOperacionException(excepcion.Message);
            }

            await _solicitudes.AgregarAsync(solicitud, token);
            foreach (var evento in solicitud.EventosDominio)
                await _outbox.EncolarAsync(evento, solicitud.Version, token);
            await _unidad.GuardarCambiosAsync(token);
            resultado = SolicitudRenovacionDto.Desde(solicitud);
        }, NivelAislamientoTransaccion.Serializable, cancellationToken);

        return resultado ?? throw new InvalidOperationException(
            "La solicitud de renovación no produjo un resultado.");
    }
}
