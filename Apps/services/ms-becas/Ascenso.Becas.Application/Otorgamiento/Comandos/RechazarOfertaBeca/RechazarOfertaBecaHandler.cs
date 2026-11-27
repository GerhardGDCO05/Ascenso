using Ascenso.Becas.Application.Comun;
using Ascenso.Becas.Application.Despacho;
using Ascenso.Becas.Application.Excepciones;
using Ascenso.Becas.Application.Otorgamiento.Puertos;
using Ascenso.Becas.Application.Otorgamiento.Servicios;
using Ascenso.Becas.Domain.Enums;
using Ascenso.Becas.Domain.Otorgamiento.Agregados;
using Ascenso.Becas.Domain.Ports;
using Microsoft.Extensions.Logging;

namespace Ascenso.Becas.Application.Otorgamiento.Comandos.RechazarOfertaBeca;

/// <summary>
/// Ejecuta el rechazo terminal de la oferta y el cierre de la beca.
/// </summary>
public sealed class RechazarOfertaBecaHandler : ICommandHandler<RechazarOfertaBecaCommand>
{
    private readonly IContextoOperacionActual _contextoActual;
    private readonly IClock _reloj;
    private readonly IMetricasOtorgamiento _metricas;
    private readonly IBecaRepository _repositorio;
    private readonly IUnitOfWork _unidadDeTrabajo;
    private readonly IOutboxBeca _outbox;
    private readonly IProgramasClient _programas;
    private readonly IIntencionAceptacionOfertaStore _intenciones;
    private readonly IProgramadorExpiracionOfertas _programador;
    private readonly ILogger<RechazarOfertaBecaHandler> _logger;

    public RechazarOfertaBecaHandler(
        IContextoOperacionActual contextoActual,
        IClock reloj,
        IMetricasOtorgamiento metricas,
        IBecaRepository repositorio,
        IUnitOfWork unidadDeTrabajo,
        IOutboxBeca outbox,
        IProgramasClient programas,
        IIntencionAceptacionOfertaStore intenciones,
        IProgramadorExpiracionOfertas programador,
        ILogger<RechazarOfertaBecaHandler> logger)
    {
        _contextoActual = contextoActual ?? throw new ArgumentNullException(nameof(contextoActual));
        _reloj = reloj ?? throw new ArgumentNullException(nameof(reloj));
        _metricas = metricas ?? throw new ArgumentNullException(nameof(metricas));
        _repositorio = repositorio ?? throw new ArgumentNullException(nameof(repositorio));
        _unidadDeTrabajo = unidadDeTrabajo ?? throw new ArgumentNullException(nameof(unidadDeTrabajo));
        _outbox = outbox ?? throw new ArgumentNullException(nameof(outbox));
        _programas = programas ?? throw new ArgumentNullException(nameof(programas));
        _intenciones = intenciones ?? throw new ArgumentNullException(nameof(intenciones));
        _programador = programador ?? throw new ArgumentNullException(nameof(programador));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task HandleAsync(
        RechazarOfertaBecaCommand command,
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

                if (await _intenciones.ObtenerAsync(command.OfertaId, token) is not null)
                    throw new ConflictoOperacionException(
                        "La oferta tiene una aceptación pendiente de coordinación.");

                var contexto = ContextoAplicacion.Crear(
                    _contextoActual,
                    _reloj,
                    command.Motivo);

                await LiberacionReservaOferta.ConfirmarAsync(beca,
                    command.OfertaId, EstadoOferta.RechazadaPorPostulante,
                    _programas, token);

                try
                {
                    beca.RechazarOferta(command.OfertaId, contexto);
                }
                catch (InvalidOperationException excepcion)
                {
                    throw new ConflictoOperacionException(excepcion.Message);
                }

                await EncolarEventosAsync(beca, token);
                await _unidadDeTrabajo.GuardarCambiosAsync(token);
            },
            cancellationToken);

        await CanceladorExpiracionOferta.CancelarAsync(
            _programador,
            _logger,
            command.OfertaId,
            referencia.Oferta?.ReferenciaJobExpiracionHangfireId);
        _metricas.OfertaRechazada();
    }

    private async Task EncolarEventosAsync(Beca beca, CancellationToken cancellationToken)
    {
        foreach (var evento in beca.EventosDominio)
        {
            await _outbox.EncolarAsync(evento, beca.Version, cancellationToken);
        }
    }
}
