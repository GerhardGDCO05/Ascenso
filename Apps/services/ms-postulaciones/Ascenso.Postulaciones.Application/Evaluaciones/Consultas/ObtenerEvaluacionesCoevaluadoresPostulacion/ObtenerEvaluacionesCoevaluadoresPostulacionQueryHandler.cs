using Ascenso.Postulaciones.Application.Common;
using Ascenso.Postulaciones.Application.Evaluaciones.DTOs;
using Ascenso.Postulaciones.Application.Interfaces;
using Ascenso.Postulaciones.Domain.Enums;
using Ascenso.Postulaciones.Domain.Excepciones;
using Ascenso.Postulaciones.Domain.Interfaces;
using FluentValidation;

namespace Ascenso.Postulaciones.Application.Evaluaciones.Consultas.ObtenerEvaluacionesCoevaluadoresPostulacion;

public sealed class ObtenerEvaluacionesCoevaluadoresPostulacionQueryHandler
{
    private readonly IUsuarioActual _usuarioActual;
    private readonly IConsultaEvaluacionesCoevaluadoresRepository _consultaCoevaluadoresRepository;
    private readonly IConvocatoriaEvaluacionClient _convocatoriaClient;
    private readonly IValidator<ObtenerEvaluacionesCoevaluadoresPostulacionQuery> _validator;
    private readonly TimeProvider _timeProvider;

    public ObtenerEvaluacionesCoevaluadoresPostulacionQueryHandler(
        IUsuarioActual usuarioActual,
        IConsultaEvaluacionesCoevaluadoresRepository consultaCoevaluadoresRepository,
        IConvocatoriaEvaluacionClient convocatoriaClient,
        IValidator<ObtenerEvaluacionesCoevaluadoresPostulacionQuery> validator,
        TimeProvider timeProvider)
    {
        _usuarioActual = usuarioActual ?? throw new ArgumentNullException(nameof(usuarioActual));
        _consultaCoevaluadoresRepository = consultaCoevaluadoresRepository
            ?? throw new ArgumentNullException(nameof(consultaCoevaluadoresRepository));
        _convocatoriaClient = convocatoriaClient ?? throw new ArgumentNullException(nameof(convocatoriaClient));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<EvaluacionesCoevaluadoresPostulacionDto> HandleAsync(
        ObtenerEvaluacionesCoevaluadoresPostulacionQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        await _validator.ValidateAndThrowAsync(query, cancellationToken);

        var usuarioEvaluadorId = _usuarioActual.ObtenerUsuarioIdRequerido();

        var asignacionPropia = await _consultaCoevaluadoresRepository
            .ObtenerAsignacionVigenteEvaluadorAsync(query.PostulacionId, usuarioEvaluadorId, cancellationToken);

        if (asignacionPropia is null
            || asignacionPropia.UsuarioEvaluadorId != usuarioEvaluadorId
            || asignacionPropia.Estado == EstadoAsignacionEvaluacion.ReasignadaPorConflicto)
        {
            throw new EvaluacionConfidencialException();
        }

        var ahora = _timeProvider.GetUtcNow();

        var ventanaRevision = await _convocatoriaClient
            .ObtenerVentanaRevisionAsync(asignacionPropia.ConvocatoriaId, cancellationToken)
            ?? throw new ConvocatoriaNoEncontradaException();

        ventanaRevision.AsegurarPeriodoCerradoParaVisibilidadCoevaluadores(ahora);

        var evaluaciones = await _consultaCoevaluadoresRepository
            .ListarDefinitivasDeCoevaluadoresAsync(query.PostulacionId, usuarioEvaluadorId, cancellationToken);

        var evaluacionesDto = evaluaciones
            .Where(evaluacion =>
                evaluacion.Estado == EstadoEvaluacion.EnviadaDefinitiva
                && evaluacion.EvaluadorId != usuarioEvaluadorId)
            .Select(evaluacion => evaluacion.ACoevaluadorDto())
            .ToArray();

        return new EvaluacionesCoevaluadoresPostulacionDto(
            query.PostulacionId,
            asignacionPropia.ConvocatoriaId,
            ventanaRevision.FechaCierreEvaluacion,
            evaluacionesDto);
    }
}
