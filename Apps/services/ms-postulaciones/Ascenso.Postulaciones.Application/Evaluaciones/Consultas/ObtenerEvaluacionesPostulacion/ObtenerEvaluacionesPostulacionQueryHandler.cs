using Ascenso.Postulaciones.Application.Evaluaciones.DTOs;
using Ascenso.Postulaciones.Domain.Enums;
using Ascenso.Postulaciones.Domain.Interfaces;
using FluentValidation;

namespace Ascenso.Postulaciones.Application.Evaluaciones.Consultas.ObtenerEvaluacionesPostulacion;

public sealed class ObtenerEvaluacionesPostulacionQueryHandler
{
    private readonly IEvaluacionRepository _evaluacionRepository;
    private readonly IValidator<ObtenerEvaluacionesPostulacionQuery> _validator;

    public ObtenerEvaluacionesPostulacionQueryHandler(
        IEvaluacionRepository evaluacionRepository,
        IValidator<ObtenerEvaluacionesPostulacionQuery> validator)
    {
        _evaluacionRepository = evaluacionRepository ?? throw new ArgumentNullException(nameof(evaluacionRepository));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
    }

    public async Task<EvaluacionesPostulacionDto> HandleAsync(
        ObtenerEvaluacionesPostulacionQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        await _validator.ValidateAndThrowAsync(query, cancellationToken);

        var evaluaciones = await _evaluacionRepository
            .ListarDefinitivasPorPostulacionAsync(query.PostulacionId, cancellationToken);

        var definitivas = evaluaciones
            .Where(evaluacion => evaluacion.Estado != EstadoEvaluacion.Borrador)
            .Select(evaluacion => evaluacion.ASupervisionDto())
            .ToArray();

        return new EvaluacionesPostulacionDto(query.PostulacionId, definitivas);
    }
}
