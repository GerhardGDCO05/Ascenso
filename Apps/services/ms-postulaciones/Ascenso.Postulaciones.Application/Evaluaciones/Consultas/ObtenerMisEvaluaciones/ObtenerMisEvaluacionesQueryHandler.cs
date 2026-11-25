using Ascenso.Postulaciones.Application.Asignaciones.DTOs;
using Ascenso.Postulaciones.Application.Common;
using Ascenso.Postulaciones.Application.Evaluaciones.DTOs;
using Ascenso.Postulaciones.Application.Interfaces;
using Ascenso.Postulaciones.Domain.Interfaces;
using FluentValidation;

namespace Ascenso.Postulaciones.Application.Evaluaciones.Consultas.ObtenerMisEvaluaciones;

public sealed class ObtenerMisEvaluacionesQueryHandler
{
    private readonly IUsuarioActual _usuarioActual;
    private readonly IEvaluacionRepository _evaluacionRepository;
    private readonly IValidator<ObtenerMisEvaluacionesQuery> _validator;

    public ObtenerMisEvaluacionesQueryHandler(
        IUsuarioActual usuarioActual,
        IEvaluacionRepository evaluacionRepository,
        IValidator<ObtenerMisEvaluacionesQuery> validator)
    {
        _usuarioActual = usuarioActual ?? throw new ArgumentNullException(nameof(usuarioActual));
        _evaluacionRepository = evaluacionRepository ?? throw new ArgumentNullException(nameof(evaluacionRepository));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
    }

    public async Task<PaginaDto<ResumenMiEvaluacionDto>> HandleAsync(
        ObtenerMisEvaluacionesQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        await _validator.ValidateAndThrowAsync(query, cancellationToken);

        var usuarioEvaluadorId = _usuarioActual.ObtenerUsuarioIdRequerido();

        var pagina = await _evaluacionRepository.ListarPorEvaluadorAsync(
            usuarioEvaluadorId,
            query.Estado,
            query.Pagina,
            query.Tamano,
            cancellationToken);

        var items = pagina.Items
            .Select(evaluacion => evaluacion.AResumenMiEvaluacionDto())
            .ToArray();

        return new PaginaDto<ResumenMiEvaluacionDto>(
            items,
            query.Pagina,
            query.Tamano,
            pagina.Total);
    }
}
