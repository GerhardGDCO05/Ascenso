using Ascenso.Becas.Application.Comun;
using Ascenso.Becas.Application.Despacho;
using Ascenso.Becas.Application.Excepciones;
using Ascenso.Becas.Application.Otorgamiento.Puertos;
using Ascenso.Becas.Domain.Otorgamiento.Agregados;
using Ascenso.Becas.Domain.Ports;

namespace Ascenso.Becas.Application.Otorgamiento.Comandos.AnularOfertaBeca;

/// <summary>
/// Ejecuta la anulación terminal de la oferta y el cierre de la beca.
/// </summary>
public sealed class AnularOfertaBecaHandler : ICommandHandler<AnularOfertaBecaCommand>
{
    private readonly IContextoOperacionActual _contextoActual;
    private readonly IClock _reloj;
    private readonly IBecaRepository _repositorio;
    private readonly IUnitOfWork _unidadDeTrabajo;
    private readonly IOutboxBeca _outbox;

    public AnularOfertaBecaHandler(
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

    public async Task HandleAsync(
        AnularOfertaBecaCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var referencia = await _repositorio.ObtenerPorOfertaAsync(command.OfertaId, cancellationToken)
            ?? throw new RecursoNoEncontradoException("oferta", command.OfertaId);

        await _unidadDeTrabajo.EjecutarEnTransaccionAsync(
            async token =>
            {
                var beca = await _repositorio.ObtenerBecaParaActualizarAsync(referencia.Id, token)
                    ?? throw new RecursoNoEncontradoException("beca", referencia.Id);

                var contexto = ContextoAplicacion.Crear(
                    _contextoActual,
                    _reloj,
                    command.Justificacion);

                try
                {
                    beca.AnularOferta(command.OfertaId, contexto);
                }
                catch (InvalidOperationException excepcion)
                {
                    throw new ConflictoOperacionException(excepcion.Message);
                }

                await EncolarEventosAsync(beca, token);
                await _unidadDeTrabajo.GuardarCambiosAsync(token);
            },
            cancellationToken);
    }

    private async Task EncolarEventosAsync(Beca beca, CancellationToken cancellationToken)
    {
        foreach (var evento in beca.EventosDominio)
        {
            await _outbox.EncolarAsync(evento, cancellationToken);
        }
    }
}
