using Ascenso.Postulaciones.Application.Common;
using Ascenso.Postulaciones.Application.Consolidacion.DTOs;
using Ascenso.Postulaciones.Application.Interfaces;
using Ascenso.Postulaciones.Domain.Consolidacion.Servicios;
using Ascenso.Postulaciones.Domain.Enums;
using Ascenso.Postulaciones.Domain.Excepciones;
using Ascenso.Postulaciones.Domain.Interfaces;
using FluentValidation;

namespace Ascenso.Postulaciones.Application.Consolidacion.Comandos.ResolverCasoLimiteRanking;

public sealed class ResolverCasoLimiteRankingCommandHandler
{
    public const string RolCoordinadorPrograma = "COORDINADOR_PROGRAMA";

    private readonly IUsuarioActual _usuarioActual;
    private readonly IRolesUsuarioActual _rolesUsuarioActual;
    private readonly IConsultaJefaturaComiteRepository _consultaJefaturaComiteRepository;
    private readonly IVerificadorConflictoInteresDecisor _verificadorConflictoInteres;
    private readonly IRankingConvocatoriaRepository _rankingRepository;
    private readonly IConsultaEstadoEvaluacionesConvocatoriaRepository _consultaEstadoEvaluacionesRepository;
    private readonly IConvocatoriaEvaluacionClient _convocatoriaClient;
    private readonly IDisponibilidadRankingClient _disponibilidadClient;
    private readonly IProveedorMontoEstimadoBeca _proveedorMonto;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<ResolverCasoLimiteRankingCommand> _validator;
    private readonly TimeProvider _timeProvider;

    public ResolverCasoLimiteRankingCommandHandler(
        IUsuarioActual usuarioActual,
        IRolesUsuarioActual rolesUsuarioActual,
        IConsultaJefaturaComiteRepository consultaJefaturaComiteRepository,
        IVerificadorConflictoInteresDecisor verificadorConflictoInteres,
        IRankingConvocatoriaRepository rankingRepository,
        IConsultaEstadoEvaluacionesConvocatoriaRepository consultaEstadoEvaluacionesRepository,
        IConvocatoriaEvaluacionClient convocatoriaClient,
        IDisponibilidadRankingClient disponibilidadClient,
        IProveedorMontoEstimadoBeca proveedorMonto,
        IUnitOfWork unitOfWork,
        IValidator<ResolverCasoLimiteRankingCommand> validator,
        TimeProvider timeProvider)
    {
        _usuarioActual = usuarioActual ?? throw new ArgumentNullException(nameof(usuarioActual));
        _rolesUsuarioActual = rolesUsuarioActual ?? throw new ArgumentNullException(nameof(rolesUsuarioActual));
        _consultaJefaturaComiteRepository = consultaJefaturaComiteRepository
            ?? throw new ArgumentNullException(nameof(consultaJefaturaComiteRepository));
        _verificadorConflictoInteres = verificadorConflictoInteres
            ?? throw new ArgumentNullException(nameof(verificadorConflictoInteres));
        _rankingRepository = rankingRepository ?? throw new ArgumentNullException(nameof(rankingRepository));
        _consultaEstadoEvaluacionesRepository = consultaEstadoEvaluacionesRepository
            ?? throw new ArgumentNullException(nameof(consultaEstadoEvaluacionesRepository));
        _convocatoriaClient = convocatoriaClient ?? throw new ArgumentNullException(nameof(convocatoriaClient));
        _disponibilidadClient = disponibilidadClient ?? throw new ArgumentNullException(nameof(disponibilidadClient));
        _proveedorMonto = proveedorMonto ?? throw new ArgumentNullException(nameof(proveedorMonto));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<RankingConvocatoriaDto> HandleAsync(
        ResolverCasoLimiteRankingCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        await _validator.ValidateAndThrowAsync(command, cancellationToken);

        var usuarioDecisorId = _usuarioActual.ObtenerUsuarioIdRequerido();

        if (!_rolesUsuarioActual.TieneRol(RolCoordinadorPrograma))
        {
            var esJefaturaActiva = await _consultaJefaturaComiteRepository
                .EsJefaturaActivaEnConvocatoriaAsync(command.ConvocatoriaId, usuarioDecisorId, cancellationToken);

            if (!esJefaturaActiva)
            {
                throw new PermisoDenegadoResolucionCasoLimiteException();
            }
        }

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

        var corteActual = ServicioCalculoRankingCorte.Calcular(
            command.ConvocatoriaId,
            resultados,
            montos,
            disponibilidad);

        if (corteActual.CasoLimiteActivo is null)
        {
            var seleccionadasSet = new HashSet<Guid>(command.PostulacionesSeleccionadasIds);
            if (resultados.Any(r => seleccionadasSet.Contains(r.PostulacionId) && r.TieneDecisionCasoLimite))
            {
                throw new CasoLimiteYaResueltoException();
            }

            throw new CasoLimiteNoDetectadoException();
        }

        var tieneConflicto = await _verificadorConflictoInteres.TieneConflictoEnPostulacionesAsync(
            command.ConvocatoriaId,
            usuarioDecisorId,
            corteActual.CasoLimiteActivo.PostulacionesAfectadas,
            cancellationToken);

        if (tieneConflicto)
        {
            throw new ConflictoInteresDecisorCasoLimiteException();
        }

        ServicioResolucionCasoLimite.AplicarResolucion(
            corteActual,
            resultados,
            command.PostulacionesSeleccionadasIds,
            command.Justificacion,
            ahora,
            usuarioDecisorId);

        await _unitOfWork.GuardarCambiosAsync(cancellationToken);

        var corteActualizado = ServicioCalculoRankingCorte.Calcular(
            command.ConvocatoriaId,
            resultados,
            montos,
            disponibilidad);

        return corteActualizado.ARankingProvisionalDto(
            filtroClasificacion: null,
            pagina: 1,
            tamano: resultados.Count);
    }
}
