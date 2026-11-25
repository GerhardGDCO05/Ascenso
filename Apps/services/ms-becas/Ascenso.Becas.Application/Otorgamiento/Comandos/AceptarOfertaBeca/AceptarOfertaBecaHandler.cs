using Ascenso.Becas.Application.Comun;
using Ascenso.Becas.Application.Despacho;
using Ascenso.Becas.Application.Excepciones;
using Ascenso.Becas.Application.Otorgamiento.Dtos;
using Ascenso.Becas.Application.Otorgamiento.Puertos;
using Ascenso.Becas.Application.Otorgamiento.Servicios;
using Ascenso.Becas.Domain.Enums;
using Ascenso.Becas.Domain.Otorgamiento.Agregados;
using Ascenso.Becas.Domain.Otorgamiento.Servicios;
using Ascenso.Becas.Domain.Ports;
using Microsoft.Extensions.Logging;

namespace Ascenso.Becas.Application.Otorgamiento.Comandos.AceptarOfertaBeca;

/// <summary>
/// Acepta la oferta dentro de una transacción serializable, con bloqueo por
/// estudiante y bloqueo de fila antes de evaluar el no doble beneficio.
/// </summary>
public sealed class AceptarOfertaBecaHandler
    : ICommandHandler<AceptarOfertaBecaCommand, OfertaRespondidaDto>
{
    private readonly IContextoOperacionActual _contextoActual;
    private readonly IClock _reloj;
    private readonly IMetricasOtorgamiento _metricas;
    private readonly IBecaRepository _repositorio;
    private readonly IUnitOfWork _unidadDeTrabajo;
    private readonly IOutboxBeca _outbox;
    private readonly IProgramadorExpiracionOfertas _programador;
    private readonly ILogger<AceptarOfertaBecaHandler> _logger;

    public AceptarOfertaBecaHandler(
        IContextoOperacionActual contextoActual,
        IClock reloj,
        IMetricasOtorgamiento metricas,
        IBecaRepository repositorio,
        IUnitOfWork unidadDeTrabajo,
        IOutboxBeca outbox,
        IProgramadorExpiracionOfertas programador,
        ILogger<AceptarOfertaBecaHandler> logger)
    {
        _contextoActual = contextoActual ?? throw new ArgumentNullException(nameof(contextoActual));
        _reloj = reloj ?? throw new ArgumentNullException(nameof(reloj));
        _metricas = metricas ?? throw new ArgumentNullException(nameof(metricas));
        _repositorio = repositorio ?? throw new ArgumentNullException(nameof(repositorio));
        _unidadDeTrabajo = unidadDeTrabajo ?? throw new ArgumentNullException(nameof(unidadDeTrabajo));
        _outbox = outbox ?? throw new ArgumentNullException(nameof(outbox));
        _programador = programador ?? throw new ArgumentNullException(nameof(programador));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<OfertaRespondidaDto> HandleAsync(
        AceptarOfertaBecaCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var referencia = await _repositorio.ObtenerPorOfertaAsync(command.OfertaId, cancellationToken)
            ?? throw new RecursoNoEncontradoException("oferta", command.OfertaId);

        OfertaRespondidaDto? resultado = null;

        await _unidadDeTrabajo.EjecutarEnTransaccionAsync(
            async token =>
            {
                await _repositorio.AdquirirBloqueoEstudianteAsync(
                    referencia.PostulanteEstudianteId,
                    token);

                var beca = await _repositorio.ObtenerBecaParaActualizarAsync(referencia.Id, token)
                    ?? throw new RecursoNoEncontradoException("beca", referencia.Id);

                var activas = await _repositorio.ObtenerActivasPorEstudianteAsync(
                    beca.PostulanteEstudianteId,
                    token);

                PoliticaNoDobleBeneficio.Validar(beca, activas);

                var contexto = ContextoAplicacion.Crear(_contextoActual, _reloj);

                try
                {
                    beca.AceptarOferta(command.OfertaId, contexto);
                }
                catch (InvalidOperationException excepcion)
                {
                    throw new ConflictoOperacionException(excepcion.Message);
                }

                await EncolarEventosAsync(beca, token);
                await _unidadDeTrabajo.GuardarCambiosAsync(token);

                resultado = new OfertaRespondidaDto(
                    command.OfertaId,
                    beca.Id,
                    beca.Oferta!.Estado.ACodigo(),
                    beca.Estado.ACodigo());
            },
            NivelAislamientoTransaccion.Serializable,
            cancellationToken);

        if (resultado is not null)
        {
            await CanceladorExpiracionOferta.CancelarAsync(
                _programador,
                _logger,
                command.OfertaId,
                referencia.Oferta?.ReferenciaJobExpiracionHangfireId);
            _metricas.OfertaAceptada();
        }

        return resultado
            ?? throw new InvalidOperationException("La aceptación no produjo un resultado.");
    }

    private async Task EncolarEventosAsync(Beca beca, CancellationToken cancellationToken)
    {
        foreach (var evento in beca.EventosDominio)
        {
            await _outbox.EncolarAsync(evento, beca.Version, cancellationToken);
        }
    }
}
