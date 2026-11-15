using Ascenso.Becas.Application.Comun;
using Ascenso.Becas.Application.Despacho;
using Ascenso.Becas.Application.Excepciones;
using Ascenso.Becas.Application.Otorgamiento.Puertos;
using Ascenso.Becas.Domain.Enums;
using Ascenso.Becas.Domain.Otorgamiento.Agregados;
using Ascenso.Becas.Domain.Ports;

namespace Ascenso.Becas.Application.Otorgamiento.Comandos.ExpirarOfertaBeca;

/// <summary>
/// Relee la oferta bajo bloqueo de fila y solo la expira si continúa pendiente
/// y la fecha de vencimiento ya pasó. No falla si la oferta ya fue respondida.
/// </summary>
public sealed class ExpirarOfertaBecaHandler
    : ICommandHandler<ExpirarOfertaBecaCommand, bool>
{
    private readonly IContextoOperacionActual _contextoActual;
    private readonly IClock _reloj;
    private readonly IBecaRepository _repositorio;
    private readonly IUnitOfWork _unidadDeTrabajo;
    private readonly IOutboxBeca _outbox;

    public ExpirarOfertaBecaHandler(
        IContextoOperacionActual contextoActual,
        IClock reloj,
        IBecaRepository repositorio,
        IUnitOfWork unidadDeTrabajo,
        IOutboxBeca outbox)
    {
        _contextoActual = contextoActual ?? throw new ArgumentNullException(nameof(contextoActual));
        _reloj = reloj ?? throw new ArgumentNullException(nameof(reloj));
        _repositorio = repositorio ?? throw new ArgumentNullException(nameof(repositorio));
        _unidadDeTrabajo = unidadDeTrabajo ?? throw new ArgumentNullException(nameof(unidadDeTrabajo));
        _outbox = outbox ?? throw new ArgumentNullException(nameof(outbox));
    }

    public async Task<bool> HandleAsync(
        ExpirarOfertaBecaCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var referencia = await _repositorio.ObtenerPorOfertaAsync(command.OfertaId, cancellationToken);

        if (referencia is null)
        {
            return false;
        }

        var expirada = false;

        await _unidadDeTrabajo.EjecutarEnTransaccionAsync(
            async token =>
            {
                var beca = await _repositorio.ObtenerBecaParaActualizarAsync(referencia.Id, token);

                if (beca?.Oferta is null
                    || beca.Oferta.Id != command.OfertaId
                    || beca.Oferta.Estado != EstadoOferta.PendienteRespuesta
                    || _reloj.UtcNow <= beca.Oferta.FechaVencimientoAceptacion)
                {
                    return;
                }

                var contexto = ContextoAplicacion.Crear(
                    _contextoActual,
                    _reloj,
                    exigirUsuario: false);

                try
                {
                    beca.ExpirarOferta(command.OfertaId, contexto);
                }
                catch (InvalidOperationException excepcion)
                {
                    throw new ConflictoOperacionException(excepcion.Message);
                }

                await EncolarEventosAsync(beca, token);
                await _unidadDeTrabajo.GuardarCambiosAsync(token);
                expirada = true;
            },
            cancellationToken);

        return expirada;
    }

    private async Task EncolarEventosAsync(Beca beca, CancellationToken cancellationToken)
    {
        foreach (var evento in beca.EventosDominio)
        {
            await _outbox.EncolarAsync(evento, cancellationToken);
        }
    }
}
