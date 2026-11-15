using Ascenso.Becas.Application.Comun;
using Ascenso.Becas.Application.Despacho;
using Ascenso.Becas.Application.Excepciones;
using Ascenso.Becas.Application.Otorgamiento.Dtos;
using Ascenso.Becas.Application.Otorgamiento.Puertos;
using Ascenso.Becas.Domain.Enums;
using Ascenso.Becas.Domain.Otorgamiento.Agregados;
using Ascenso.Becas.Domain.Ports;

namespace Ascenso.Becas.Application.Otorgamiento.Comandos.RevisarCompromisoBecario;

/// <summary>
/// Revisa el compromiso bajo bloqueo de fila de la beca y actualiza la
/// habilitación de desembolsos según la decisión.
/// </summary>
public sealed class RevisarCompromisoBecarioHandler
    : ICommandHandler<RevisarCompromisoBecarioCommand, CompromisoRevisadoDto>
{
    private readonly IContextoOperacionActual _contextoActual;
    private readonly IClock _reloj;
    private readonly IBecaRepository _repositorio;
    private readonly ICompromisoBecarioRepository _compromisos;
    private readonly IUnitOfWork _unidadDeTrabajo;
    private readonly IOutboxBeca _outbox;

    public RevisarCompromisoBecarioHandler(
        IContextoOperacionActual contextoActual,
        IClock reloj,
        IBecaRepository repositorio,
        ICompromisoBecarioRepository compromisos,
        IUnitOfWork unidadDeTrabajo,
        IOutboxBeca outbox)
    {
        _contextoActual = contextoActual ?? throw new ArgumentNullException(nameof(contextoActual));
        _reloj = reloj ?? throw new ArgumentNullException(nameof(reloj));
        _repositorio = repositorio ?? throw new ArgumentNullException(nameof(repositorio));
        _compromisos = compromisos ?? throw new ArgumentNullException(nameof(compromisos));
        _unidadDeTrabajo = unidadDeTrabajo ?? throw new ArgumentNullException(nameof(unidadDeTrabajo));
        _outbox = outbox ?? throw new ArgumentNullException(nameof(outbox));
    }

    public async Task<CompromisoRevisadoDto> HandleAsync(
        RevisarCompromisoBecarioCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var compromiso = await _compromisos.ObtenerPorIdAsync(
            command.CompromisoId,
            cancellationToken)
            ?? throw new RecursoNoEncontradoException("compromiso", command.CompromisoId);

        CompromisoRevisadoDto? resultado = null;

        await _unidadDeTrabajo.EjecutarEnTransaccionAsync(
            async token =>
            {
                var beca = await _repositorio.ObtenerBecaParaActualizarAsync(
                    compromiso.BecaId,
                    token)
                    ?? throw new RecursoNoEncontradoException("beca", compromiso.BecaId);

                var contexto = ContextoAplicacion.Crear(
                    _contextoActual,
                    _reloj,
                    command.Observaciones);

                try
                {
                    if (command.Aprobar)
                    {
                        beca.AprobarCompromiso(command.Observaciones, contexto);
                    }
                    else
                    {
                        beca.RechazarCompromiso(command.Observaciones!, contexto);
                    }
                }
                catch (ArgumentException excepcion)
                {
                    throw new SolicitudInvalidaException(excepcion.Message);
                }
                catch (InvalidOperationException excepcion)
                {
                    throw new ConflictoOperacionException(excepcion.Message);
                }

                await EncolarEventosAsync(beca, token);
                await _unidadDeTrabajo.GuardarCambiosAsync(token);

                resultado = new CompromisoRevisadoDto(
                    beca.Compromiso!.Id,
                    beca.Compromiso.Estado.ACodigo(),
                    beca.HabilitadaParaDesembolso);
            },
            cancellationToken);

        return resultado
            ?? throw new InvalidOperationException("La revisión del compromiso no produjo un resultado.");
    }

    private async Task EncolarEventosAsync(Beca beca, CancellationToken cancellationToken)
    {
        foreach (var evento in beca.EventosDominio)
        {
            await _outbox.EncolarAsync(evento, cancellationToken);
        }
    }
}
