using Ascenso.Postulaciones.Application.Asignaciones.DTOs;
using Ascenso.Postulaciones.Application.Common;
using Ascenso.Postulaciones.Application.Interfaces;
using Ascenso.Postulaciones.Domain.Interfaces;
using FluentValidation;

namespace Ascenso.Postulaciones.Application.Asignaciones.Consultas.ObtenerMisAsignaciones;

public sealed class ObtenerMisAsignacionesQueryHandler
{
    private readonly IUsuarioActual _usuarioActual;
    private readonly IAsignacionEvaluacionRepository _asignacionRepository;
    private readonly IValidator<ObtenerMisAsignacionesQuery> _validator;

    public ObtenerMisAsignacionesQueryHandler(
        IUsuarioActual usuarioActual,
        IAsignacionEvaluacionRepository asignacionRepository,
        IValidator<ObtenerMisAsignacionesQuery> validator)
    {
        _usuarioActual = usuarioActual ?? throw new ArgumentNullException(nameof(usuarioActual));
        _asignacionRepository = asignacionRepository ?? throw new ArgumentNullException(nameof(asignacionRepository));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
    }

    public async Task<PaginaDto<MiAsignacionDto>> HandleAsync(
        ObtenerMisAsignacionesQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        await _validator.ValidateAndThrowAsync(query, cancellationToken);

        var usuarioEvaluadorId = _usuarioActual.ObtenerUsuarioIdRequerido();

        var pagina = await _asignacionRepository.ListarPorEvaluadorAsync(
            usuarioEvaluadorId,
            query.Estado,
            query.Pagina,
            query.Tamano,
            cancellationToken);

        var items = pagina.Items
            .Select(asignacion => asignacion.AMiAsignacionDto())
            .ToArray();

        return new PaginaDto<MiAsignacionDto>(
            items,
            query.Pagina,
            query.Tamano,
            pagina.Total);
    }
}
