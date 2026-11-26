using Ascenso.Postulaciones.Application.Common;
using Ascenso.Postulaciones.Application.Consolidacion.DTOs;
using Ascenso.Postulaciones.Application.Interfaces;
using Ascenso.Postulaciones.Domain.Comun;
using Ascenso.Postulaciones.Domain.Consolidacion.Agregados;
using Ascenso.Postulaciones.Domain.Consolidacion.ObjetosValor;
using Ascenso.Postulaciones.Domain.Consolidacion.Servicios;
using Ascenso.Postulaciones.Domain.Enums;
using Ascenso.Postulaciones.Domain.Excepciones;
using Ascenso.Postulaciones.Domain.Interfaces;
using FluentValidation;

namespace Ascenso.Postulaciones.Application.Consolidacion.Comandos.ConsolidarPostulacion;

public sealed class ConsolidarPostulacionCommandHandler
{
    private readonly IUsuarioActual _usuarioActual;
    private readonly IResultadoConsolidadoRepository _resultadoRepository;
    private readonly IConsultaDatosConsolidacionRepository _consultaDatosRepository;
    private readonly IProveedorParametrosConsolidacion _proveedorParametros;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<ConsolidarPostulacionCommand> _validator;
    private readonly TimeProvider _timeProvider;

    public ConsolidarPostulacionCommandHandler(
        IUsuarioActual usuarioActual,
        IResultadoConsolidadoRepository resultadoRepository,
        IConsultaDatosConsolidacionRepository consultaDatosRepository,
        IProveedorParametrosConsolidacion proveedorParametros,
        IUnitOfWork unitOfWork,
        IValidator<ConsolidarPostulacionCommand> validator,
        TimeProvider timeProvider)
    {
        _usuarioActual = usuarioActual ?? throw new ArgumentNullException(nameof(usuarioActual));
        _resultadoRepository = resultadoRepository ?? throw new ArgumentNullException(nameof(resultadoRepository));
        _consultaDatosRepository = consultaDatosRepository ?? throw new ArgumentNullException(nameof(consultaDatosRepository));
        _proveedorParametros = proveedorParametros ?? throw new ArgumentNullException(nameof(proveedorParametros));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<ResultadoConsolidarPostulacionDto> HandleAsync(
        ConsolidarPostulacionCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        await _validator.ValidateAndThrowAsync(command, cancellationToken);

        var usuarioActorId = _usuarioActual.ObtenerUsuarioIdRequerido();

        var asignacionesVigentes = await _consultaDatosRepository
            .ListarAsignacionesVigentesPorPostulacionAsync(command.PostulacionId, cancellationToken);

        var evaluacionesDefinitivas = await _consultaDatosRepository
            .ListarEvaluacionesDefinitivasPorPostulacionAsync(command.PostulacionId, cancellationToken);

        var excepcion = await _consultaDatosRepository
            .ObtenerExcepcionEvaluadorUnicoPorPostulacionAsync(command.PostulacionId, cancellationToken);

        if (asignacionesVigentes.Any(a => a.Estado != EstadoAsignacionEvaluacion.Completada))
        {
            throw new EvaluacionesPendientesParaConsolidarException();
        }

        if (evaluacionesDefinitivas.Count == 0)
        {
            throw new EvaluadoresInsuficientesException(
                "La postulación no cuenta con evaluaciones definitivas para consolidar.");
        }

        var referencia = evaluacionesDefinitivas[0];
        var parametros = await _proveedorParametros.ObtenerParametrosAsync(
            referencia.ConvocatoriaId,
            referencia.RubricaEvaluacionId,
            cancellationToken);

        var calculo = ServicioCalculoConsolidacion.Calcular(
            command.PostulacionId,
            evaluacionesDefinitivas,
            excepcion,
            parametros);

        return await PersistirConResolucionC5Async(
            command.PostulacionId,
            calculo,
            usuarioActorId,
            cancellationToken);
    }

    public async Task<ResultadoConsolidarPostulacionDto?> IntentarConsolidarAutomaticamenteAsync(
        Guid postulacionId,
        CancellationToken cancellationToken = default)
    {
        if (postulacionId == Guid.Empty)
        {
            return null;
        }

        var asignacionesVigentes = await _consultaDatosRepository
            .ListarAsignacionesVigentesPorPostulacionAsync(postulacionId, cancellationToken);

        if (asignacionesVigentes.Count == 0
            || asignacionesVigentes.Any(a => a.Estado != EstadoAsignacionEvaluacion.Completada))
        {
            return null;
        }

        var evaluacionesDefinitivas = await _consultaDatosRepository
            .ListarEvaluacionesDefinitivasPorPostulacionAsync(postulacionId, cancellationToken);

        if (evaluacionesDefinitivas.Count == 0)
        {
            return null;
        }

        var excepcion = await _consultaDatosRepository
            .ObtenerExcepcionEvaluadorUnicoPorPostulacionAsync(postulacionId, cancellationToken);

        var referencia = evaluacionesDefinitivas[0];
        var parametros = await _proveedorParametros.ObtenerParametrosAsync(
            referencia.ConvocatoriaId,
            referencia.RubricaEvaluacionId,
            cancellationToken);

        var cumpleMinimo = evaluacionesDefinitivas.Count >= parametros.EvaluadoresMinimosRequeridos
            || (evaluacionesDefinitivas.Count == 1 && excepcion is not null);

        if (!cumpleMinimo)
        {
            return null;
        }

        var calculo = ServicioCalculoConsolidacion.Calcular(
            postulacionId,
            evaluacionesDefinitivas,
            excepcion,
            parametros);

        return await PersistirConResolucionC5Async(
            postulacionId,
            calculo,
            UsuarioSistema.Id,
            cancellationToken);
    }

    private async Task<ResultadoConsolidarPostulacionDto> PersistirConResolucionC5Async(
        Guid postulacionId,
        ResultadoCalculoConsolidacion calculo,
        Guid usuarioActorId,
        CancellationToken cancellationToken)
    {
        var ahora = _timeProvider.GetUtcNow();
        var existente = await _resultadoRepository
            .ObtenerParaActualizarPorPostulacionAsync(postulacionId, cancellationToken);

        try
        {
            if (existente is null)
            {
                var nuevo = ResultadoConsolidadoRanking.Crear(calculo, ahora, usuarioActorId);
                await _resultadoRepository.AgregarAsync(nuevo, cancellationToken);
                await _unitOfWork.GuardarCambiosAsync(cancellationToken);

                return new ResultadoConsolidarPostulacionDto(nuevo.ADto(), EsCreacion: true);
            }

            var cambio = existente.Recalcular(calculo, ahora, usuarioActorId);
            if (!cambio)
            {
                return new ResultadoConsolidarPostulacionDto(existente.ADto(), EsCreacion: false);
            }

            await _unitOfWork.GuardarCambiosAsync(cancellationToken);
            return new ResultadoConsolidarPostulacionDto(existente.ADto(), EsCreacion: false);
        }
        catch (Exception excepcion) when (excepcion is ResultadoConsolidadoDuplicadoException
            or ExcepcionConflictoConcurrencia)
        {
            return await ResolverCarreraC5Async(postulacionId, usuarioActorId, cancellationToken);
        }
    }

    private async Task<ResultadoConsolidarPostulacionDto> ResolverCarreraC5Async(
        Guid postulacionId,
        Guid usuarioActorId,
        CancellationToken cancellationToken)
    {
        var actual = await _resultadoRepository
            .RecargarParaActualizarPorPostulacionAsync(postulacionId, cancellationToken);

        if (actual is null)
        {
            throw new ExcepcionConflictoConcurrencia();
        }

        var calculoActual = await RecalcularDesdePersistenciaAsync(postulacionId, cancellationToken);

        if (actual.EstadoConfirmacionRanking == EstadoRanking.Confirmado)
        {
            throw new RankingYaConfirmadoException();
        }

        if (actual.CoincideConCalculo(calculoActual))
        {
            return new ResultadoConsolidarPostulacionDto(actual.ADto(), EsCreacion: false);
        }

        var cambio = actual.Recalcular(calculoActual, _timeProvider.GetUtcNow(), usuarioActorId);
        if (!cambio)
        {
            return new ResultadoConsolidarPostulacionDto(actual.ADto(), EsCreacion: false);
        }

        try
        {
            await _unitOfWork.GuardarCambiosAsync(cancellationToken);
            return new ResultadoConsolidarPostulacionDto(actual.ADto(), EsCreacion: false);
        }
        catch (Exception excepcion) when (excepcion is ResultadoConsolidadoDuplicadoException
            or ExcepcionConflictoConcurrencia)
        {
            var verificacion = await _resultadoRepository
                .RecargarParaActualizarPorPostulacionAsync(postulacionId, cancellationToken);

            if (verificacion is not null)
            {
                var calculoFinal = await RecalcularDesdePersistenciaAsync(postulacionId, cancellationToken);
                if (verificacion.EstadoConfirmacionRanking == EstadoRanking.Provisional
                    && verificacion.CoincideConCalculo(calculoFinal))
                {
                    return new ResultadoConsolidarPostulacionDto(verificacion.ADto(), EsCreacion: false);
                }
            }

            throw;
        }
    }

    private async Task<ResultadoCalculoConsolidacion> RecalcularDesdePersistenciaAsync(
        Guid postulacionId,
        CancellationToken cancellationToken)
    {
        var evaluacionesActuales = await _consultaDatosRepository
            .ListarEvaluacionesDefinitivasPorPostulacionAsync(postulacionId, cancellationToken);

        var excepcionActual = await _consultaDatosRepository
            .ObtenerExcepcionEvaluadorUnicoPorPostulacionAsync(postulacionId, cancellationToken);

        if (evaluacionesActuales.Count == 0)
        {
            throw new EvaluadoresInsuficientesException(
                "La postulación no cuenta con evaluaciones definitivas para consolidar.");
        }

        var referencia = evaluacionesActuales[0];
        var parametrosActuales = await _proveedorParametros.ObtenerParametrosAsync(
            referencia.ConvocatoriaId,
            referencia.RubricaEvaluacionId,
            cancellationToken);

        return ServicioCalculoConsolidacion.Calcular(
            postulacionId,
            evaluacionesActuales,
            excepcionActual,
            parametrosActuales);
    }
}
