using Ascenso.Postulaciones.Application.Asignaciones.DTOs;
using Ascenso.Postulaciones.Application.Common;
using Ascenso.Postulaciones.Application.Interfaces;
using Ascenso.Postulaciones.Domain.Interfaces;
using FluentValidation;

namespace Ascenso.Postulaciones.Application.Asignaciones.Consultas.ObtenerAsignacionesPostulacion;

public sealed class ObtenerAsignacionesPostulacionQueryHandler
{
    private readonly IUsuarioActual _usuarioActual;
    private readonly IAsignacionEvaluacionRepository _asignacionRepository;
    private readonly IValidator<ObtenerAsignacionesPostulacionQuery> _validator;

    public ObtenerAsignacionesPostulacionQueryHandler(
        IUsuarioActual usuarioActual,
        IAsignacionEvaluacionRepository asignacionRepository,
        IValidator<ObtenerAsignacionesPostulacionQuery> validator)
    {
        _usuarioActual = usuarioActual ?? throw new ArgumentNullException(nameof(usuarioActual));
        _asignacionRepository = asignacionRepository ?? throw new ArgumentNullException(nameof(asignacionRepository));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
    }

    public async Task<AsignacionesPostulacionDto> HandleAsync(
        ObtenerAsignacionesPostulacionQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        await _validator.ValidateAndThrowAsync(query, cancellationToken);

        _ = _usuarioActual.ObtenerUsuarioIdRequerido();

        var asignaciones = await _asignacionRepository
            .ListarPorPostulacionAsync(query.PostulacionId, cancellationToken);

        var items = asignaciones
            .OrderBy(asignacion => asignacion.FechaAsignacion)
            .ThenBy(asignacion => asignacion.Id)
            .Select(asignacion => asignacion.ADto())
            .ToArray();

        return new AsignacionesPostulacionDto(query.PostulacionId, items);
    }
}
