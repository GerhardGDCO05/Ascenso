using Ascenso.Postulaciones.Application.Common;
using Ascenso.Postulaciones.Application.Consolidacion.DTOs;
using Ascenso.Postulaciones.Application.Interfaces;
using Ascenso.Postulaciones.Application.ListaEspera.Interfaces;
using Ascenso.Postulaciones.Domain.Consolidacion.Servicios;
using Ascenso.Postulaciones.Domain.Enums;
using Ascenso.Postulaciones.Domain.Excepciones;
using Ascenso.Postulaciones.Domain.Interfaces;
using FluentValidation;

namespace Ascenso.Postulaciones.Application.Consolidacion.Comandos.ConfirmarRankingConvocatoria;

public sealed class ConfirmarRankingConvocatoriaCommandHandler
{
    private readonly IUsuarioActual _usuarioActual;
    private readonly IRankingConvocatoriaRepository _rankingRepository;
    private readonly IConsultaEstadoEvaluacionesConvocatoriaRepository _consultaEstadoEvaluacionesRepository;
    private readonly IConvocatoriaEvaluacionClient _convocatoriaClient;
    private readonly IDisponibilidadRankingClient _disponibilidadClient;
    private readonly IProveedorMontoEstimadoBeca _proveedorMonto;
    private readonly IGeneradorListaEsperaRanking _generadorListaEspera;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<ConfirmarRankingConvocatoriaCommand> _validator;
    private readonly TimeProvider _timeProvider;

    public ConfirmarRankingConvocatoriaCommandHandler(
        IUsuarioActual usuarioActual,
        IRankingConvocatoriaRepository rankingRepository,
        IConsultaEstadoEvaluacionesConvocatoriaRepository consultaEstadoEvaluacionesRepository,
        IConvocatoriaEvaluacionClient convocatoriaClient,
        IDisponibilidadRankingClient disponibilidadClient,
        IProveedorMontoEstimadoBeca proveedorMonto,
        IGeneradorListaEsperaRanking generadorListaEspera,
        IUnitOfWork unitOfWork,
        IValidator<ConfirmarRankingConvocatoriaCommand> validator,
        TimeProvider timeProvider)
    {
        _usuarioActual = usuarioActual ?? throw new ArgumentNullException(nameof(usuarioActual));
        _rankingRepository = rankingRepository ?? throw new ArgumentNullException(nameof(rankingRepository));
        _consultaEstadoEvaluacionesRepository = consultaEstadoEvaluacionesRepository
            ?? throw new ArgumentNullException(nameof(consultaEstadoEvaluacionesRepository));
        _convocatoriaClient = convocatoriaClient ?? throw new ArgumentNullException(nameof(convocatoriaClient));
        _disponibilidadClient = disponibilidadClient ?? throw new ArgumentNullException(nameof(disponibilidadClient));
        _proveedorMonto = proveedorMonto ?? throw new ArgumentNullException(nameof(proveedorMonto));
        _generadorListaEspera = generadorListaEspera ?? throw new ArgumentNullException(nameof(generadorListaEspera));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<RankingConvocatoriaDto> HandleAsync(
        ConfirmarRankingConvocatoriaCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        await _validator.ValidateAndThrowAsync(command, cancellationToken);

        var usuarioConfirmadorId = _usuarioActual.ObtenerUsuarioIdRequerido();

        var resultados = await _rankingRepository.ListarParaConfirmarPorConvocatoriaAsync(
            command.ConvocatoriaId,
            cancellationToken);

        if (resultados.Count == 0)
        {
            throw new RankingSinResultadosConsolidadosException();
        }

        if (resultados.Any(r => r.EstadoConfirmacionRanking == EstadoRanking.Confirmado))
        {
            throw new RankingYaConfirmadoException();
        }

        var existenPendientes = await _consultaEstadoEvaluacionesRepository
            .ExistenEvaluacionesPendientesOSinConsolidarAsync(command.ConvocatoriaId, cancellationToken);

        if (existenPendientes)
        {
            throw new EvaluacionesPendientesParaConsolidarException(
                "La convocatoria aún tiene evaluaciones pendientes de completar o postulaciones sin consolidar.");
        }

        var ventana = await _convocatoriaClient.ObtenerVentanaRevisionAsync(
            command.ConvocatoriaId,
            cancellationToken)
            ?? throw new ConvocatoriaNoEncontradaException();

        var ahora = _timeProvider.GetUtcNow();
        switch (ventana.ObtenerEstadoPeriodoRevision(ahora))
        {
            case EstadoPeriodoRevision.Cerrado:
                break;
            case EstadoPeriodoRevision.Cancelado:
                throw new ConvocatoriaCanceladaException();
            case EstadoPeriodoRevision.NoIniciado:
                throw new PeriodoRevisionNoIniciadoException();
            case EstadoPeriodoRevision.Abierto:
                throw new PeriodoRevisionAbiertoException();
            default:
                throw new InvalidOperationException("Estado del periodo de revisión no soportado.");
        }

        var disponibilidad = await _disponibilidadClient.ObtenerDisponibilidadPorConvocatoriaAsync(
            command.ConvocatoriaId,
            cancellationToken)
            ?? throw new ConvocatoriaNoEncontradaException();

        var montos = await _proveedorMonto.ObtenerMontosEstimadosPorPostulacionAsync(
            command.ConvocatoriaId,
            disponibilidad.ProgramaBecaId,
            resultados.Select(r => r.PostulacionId).ToArray(),
            cancellationToken);

        var corte = ServicioCalculoRankingCorte.Calcular(
            command.ConvocatoriaId,
            resultados,
            montos,
            disponibilidad);

        if (corte.TieneEmpateEnLineaDeCorte)
        {
            throw new EmpatesSinResolverEnCorteException();
        }

        if (corte.TieneDecisionCasoLimiteInvalidaPorDisponibilidad)
        {
            throw new DecisionCasoLimiteInvalidaPorDisponibilidadException();
        }

        if (corte.TieneAdvertenciaDiscrepanciasEnCorte && !command.ConfirmarConAdvertenciaDiscrepancias)
        {
            throw new DiscrepanciasSinRevisarEnCorteException();
        }

        var resultadosPorId = resultados.ToDictionary(r => r.Id);
        foreach (var elemento in corte.Elementos)
        {
            resultadosPorId[elemento.ResultadoConsolidadoId].ConfirmarRanking(
                elemento.PosicionRanking,
                elemento.MontoEstimadoBeca,
                elemento.Clasificacion,
                ahora,
                usuarioConfirmadorId);
        }

        await _generadorListaEspera.GenerarDesdeRankingConfirmadoAsync(
            command.ConvocatoriaId,
            disponibilidad.ProgramaBecaId,
            resultados,
            ahora,
            usuarioConfirmadorId,
            cancellationToken);

        await _unitOfWork.GuardarCambiosAsync(cancellationToken);

        return RankingMapeador.ARankingConfirmadoDto(
            command.ConvocatoriaId,
            resultados,
            disponibilidad: disponibilidad,
            filtroClasificacion: null,
            pagina: 1,
            tamano: resultados.Count);
    }
}
