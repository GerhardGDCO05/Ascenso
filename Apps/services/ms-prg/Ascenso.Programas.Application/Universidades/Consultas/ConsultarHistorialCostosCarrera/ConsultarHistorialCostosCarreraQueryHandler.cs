using Ascenso.Programas.Application.Universidades.DTOs;
using Ascenso.Programas.Domain.Excepciones;
using Ascenso.Programas.Domain.Interfaces;

namespace Ascenso.Programas.Application.Universidades.Consultas.ConsultarHistorialCostosCarrera;

public sealed class ConsultarHistorialCostosCarreraQueryHandler
{
    private readonly IUniversidadAliadaRepository _universidadRepository;

    public ConsultarHistorialCostosCarreraQueryHandler(IUniversidadAliadaRepository universidadRepository)
    {
        _universidadRepository = universidadRepository ?? throw new ArgumentNullException(nameof(universidadRepository));
    }

    public async Task<IReadOnlyList<CostoMatriculaReferenciaDto>> HandleAsync(
        ConsultarHistorialCostosCarreraQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (query.UniversidadAliadaId == Guid.Empty)
        {
            throw new ArgumentException("El identificador de la universidad aliada es obligatorio.", nameof(query));
        }

        if (query.CarreraElegibleId == Guid.Empty)
        {
            throw new ArgumentException("El identificador de la carrera elegible es obligatorio.", nameof(query));
        }

        var universidad = await _universidadRepository.ObtenerPorIdAsync(query.UniversidadAliadaId, cancellationToken)
            ?? throw new UniversidadAliadaNoEncontradaException(query.UniversidadAliadaId);

        if (universidad.Carreras.All(carrera => carrera.Id != query.CarreraElegibleId))
        {
            throw new CarreraElegibleNoEncontradaException(query.CarreraElegibleId);
        }

        var historial = await _universidadRepository.ConsultarHistorialCostosCarreraAsync(
            query.UniversidadAliadaId,
            query.CarreraElegibleId,
            query.PeriodoAcademicoId,
            cancellationToken);

        return historial.Select(costo => costo.ADto()).ToList();
    }
}
