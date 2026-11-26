using Ascenso.Postulaciones.Application.Asignaciones.DTOs;
using Ascenso.Postulaciones.Application.Common;
using Ascenso.Postulaciones.Application.Interfaces;
using Ascenso.Postulaciones.Domain.Excepciones;
using Ascenso.Postulaciones.Domain.Interfaces;
using FluentValidation;

namespace Ascenso.Postulaciones.Application.Asignaciones.Consultas.ObtenerCargaEvaluadoresComite;

public sealed class ObtenerCargaEvaluadoresComiteQueryHandler
{
    private readonly IUsuarioActual _usuarioActual;
    private readonly IComiteEvaluadorRepository _comiteRepository;
    private readonly IAsignacionEvaluacionRepository _asignacionRepository;
    private readonly IValidator<ObtenerCargaEvaluadoresComiteQuery> _validator;

    public ObtenerCargaEvaluadoresComiteQueryHandler(
        IUsuarioActual usuarioActual,
        IComiteEvaluadorRepository comiteRepository,
        IAsignacionEvaluacionRepository asignacionRepository,
        IValidator<ObtenerCargaEvaluadoresComiteQuery> validator)
    {
        _usuarioActual = usuarioActual ?? throw new ArgumentNullException(nameof(usuarioActual));
        _comiteRepository = comiteRepository ?? throw new ArgumentNullException(nameof(comiteRepository));
        _asignacionRepository = asignacionRepository ?? throw new ArgumentNullException(nameof(asignacionRepository));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
    }

    public async Task<IReadOnlyList<CargaEvaluadorDto>> HandleAsync(
        ObtenerCargaEvaluadoresComiteQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        await _validator.ValidateAndThrowAsync(query, cancellationToken);

        _ = _usuarioActual.ObtenerUsuarioIdRequerido();

        var comite = await _comiteRepository
            .ObtenerPorIdAsync(query.ComiteId, cancellationToken)
            ?? throw new ComiteNoEncontradoException();

        var cargaPorMiembro = await _asignacionRepository
            .ObtenerCargaPorMiembroAsync(comite.Id, cancellationToken);

        return comite.Miembros
            .Where(miembro => miembro.Activo)
            .OrderBy(miembro => cargaPorMiembro.TryGetValue(miembro.Id, out var carga) ? carga : 0)
            .ThenBy(miembro => miembro.FechaAsignacion)
            .ThenBy(miembro => miembro.Id)
            .Select(miembro => miembro.ACargaDto(
                cargaPorMiembro.TryGetValue(miembro.Id, out var carga) ? carga : 0))
            .ToArray();
    }
}
