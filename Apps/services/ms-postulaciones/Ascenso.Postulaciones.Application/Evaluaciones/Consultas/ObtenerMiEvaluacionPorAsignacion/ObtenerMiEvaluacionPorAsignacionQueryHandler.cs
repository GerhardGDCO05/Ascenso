using Ascenso.Postulaciones.Application.Common;
using Ascenso.Postulaciones.Application.Evaluaciones.DTOs;
using Ascenso.Postulaciones.Application.Interfaces;
using Ascenso.Postulaciones.Domain.Excepciones;
using Ascenso.Postulaciones.Domain.Interfaces;
using FluentValidation;

namespace Ascenso.Postulaciones.Application.Evaluaciones.Consultas.ObtenerMiEvaluacionPorAsignacion;

public sealed class ObtenerMiEvaluacionPorAsignacionQueryHandler
{
    private readonly IUsuarioActual _usuarioActual;
    private readonly IConsultaAsignacionParaEvaluacionRepository _asignacionRepository;
    private readonly IEvaluacionRepository _evaluacionRepository;
    private readonly IValidator<ObtenerMiEvaluacionPorAsignacionQuery> _validator;

    public ObtenerMiEvaluacionPorAsignacionQueryHandler(
        IUsuarioActual usuarioActual,
        IConsultaAsignacionParaEvaluacionRepository asignacionRepository,
        IEvaluacionRepository evaluacionRepository,
        IValidator<ObtenerMiEvaluacionPorAsignacionQuery> validator)
    {
        _usuarioActual = usuarioActual ?? throw new ArgumentNullException(nameof(usuarioActual));
        _asignacionRepository = asignacionRepository ?? throw new ArgumentNullException(nameof(asignacionRepository));
        _evaluacionRepository = evaluacionRepository ?? throw new ArgumentNullException(nameof(evaluacionRepository));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
    }

    public async Task<EvaluacionDto> HandleAsync(
        ObtenerMiEvaluacionPorAsignacionQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        await _validator.ValidateAndThrowAsync(query, cancellationToken);

        var usuarioEvaluadorId = _usuarioActual.ObtenerUsuarioIdRequerido();

        var asignacion = await _asignacionRepository
            .ObtenerPorIdAsync(query.AsignacionEvaluacionId, cancellationToken)
            ?? throw new AsignacionEvaluacionNoEncontradaException();

        if (asignacion.UsuarioEvaluadorId != usuarioEvaluadorId)
        {
            throw new EvaluacionConfidencialException();
        }

        var evaluacion = await _evaluacionRepository
            .ObtenerPorAsignacionAsync(asignacion.Id, cancellationToken)
            ?? throw new EvaluacionNoEncontradaException();

        return evaluacion.ADto();
    }
}
