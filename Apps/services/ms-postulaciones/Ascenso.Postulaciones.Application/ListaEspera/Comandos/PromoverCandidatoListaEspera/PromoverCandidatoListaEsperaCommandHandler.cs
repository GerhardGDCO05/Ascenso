using Ascenso.Postulaciones.Application.Common;
using Ascenso.Postulaciones.Application.Consolidacion.DTOs;
using Ascenso.Postulaciones.Application.Interfaces;
using Ascenso.Postulaciones.Application.ListaEspera.DTOs;
using Ascenso.Postulaciones.Domain.Excepciones;
using Ascenso.Postulaciones.Domain.Interfaces;
using Ascenso.Postulaciones.Domain.ListaEspera.Servicios;
using FluentValidation;

namespace Ascenso.Postulaciones.Application.ListaEspera.Comandos.PromoverCandidatoListaEspera;

public sealed class PromoverCandidatoListaEsperaCommandHandler
{
    private readonly IUsuarioActual _usuarioActual;
    private readonly IGestorConcurrenciaListaEspera _gestorConcurrencia;
    private readonly IResultadoConsolidadoRepository _resultadoRepository;
    private readonly IDisponibilidadRankingClient _disponibilidadClient;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<PromoverCandidatoListaEsperaCommand> _validator;
    private readonly TimeProvider _timeProvider;

    public PromoverCandidatoListaEsperaCommandHandler(
        IUsuarioActual usuarioActual,
        IGestorConcurrenciaListaEspera gestorConcurrencia,
        IResultadoConsolidadoRepository resultadoRepository,
        IDisponibilidadRankingClient disponibilidadClient,
        IUnitOfWork unitOfWork,
        IValidator<PromoverCandidatoListaEsperaCommand> validator,
        TimeProvider timeProvider)
    {
        _usuarioActual = usuarioActual ?? throw new ArgumentNullException(nameof(usuarioActual));
        _gestorConcurrencia = gestorConcurrencia ?? throw new ArgumentNullException(nameof(gestorConcurrencia));
        _resultadoRepository = resultadoRepository ?? throw new ArgumentNullException(nameof(resultadoRepository));
        _disponibilidadClient = disponibilidadClient ?? throw new ArgumentNullException(nameof(disponibilidadClient));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<PromocionListaEsperaDto> HandleAsync(
        PromoverCandidatoListaEsperaCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        await _validator.ValidateAndThrowAsync(command, cancellationToken);

        var usuarioPromotorId = _usuarioActual.ObtenerUsuarioIdRequerido();

        return await _gestorConcurrencia.EjecutarConBloqueoConvocatoriaAsync(
            command.ConvocatoriaId,
            async (entradasBloqueadas, ct) =>
            {
                var candidato = ServicioPromocionListaEspera.ResolverCandidatoAPromover(
                    entradasBloqueadas,
                    command.EntradaListaEsperaId);

                var resultado = await _resultadoRepository.ObtenerPorPostulacionAsync(
                    candidato.PostulacionId,
                    ct);
                var montoRequerido = resultado?.MontoEstimadoBeca ?? candidato.MontoEstimadoBecaSnapshot;

                var disponibilidad = await _disponibilidadClient.ObtenerDisponibilidadPorConvocatoriaAsync(
                    command.ConvocatoriaId,
                    ct)
                    ?? throw new ConvocatoriaNoEncontradaException();

                ServicioPromocionListaEspera.ValidarDisponibilidadParaPromocion(disponibilidad, montoRequerido);

                candidato.Promover(_timeProvider.GetUtcNow(), usuarioPromotorId, command.MotivoPromocion);

                await _unitOfWork.GuardarCambiosAsync(ct);

                return new PromocionListaEsperaDto(
                    candidato.ADto(),
                    disponibilidad.ADto(),
                    HabilitadaParaAdjudicacion: true);
            },
            cancellationToken);
    }
}
