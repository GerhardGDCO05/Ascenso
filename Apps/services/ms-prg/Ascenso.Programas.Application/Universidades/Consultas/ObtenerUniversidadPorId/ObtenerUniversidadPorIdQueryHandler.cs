using Ascenso.Programas.Application.Universidades.DTOs;
using Ascenso.Programas.Domain.Excepciones;
using Ascenso.Programas.Domain.Interfaces;

namespace Ascenso.Programas.Application.Universidades.Consultas.ObtenerUniversidadPorId;

public sealed class ObtenerUniversidadPorIdQueryHandler
{
    private readonly IUniversidadAliadaRepository _universidadRepository;

    public ObtenerUniversidadPorIdQueryHandler(IUniversidadAliadaRepository universidadRepository)
    {
        _universidadRepository = universidadRepository ?? throw new ArgumentNullException(nameof(universidadRepository));
    }

    public async Task<UniversidadDetalleDto> HandleAsync(
        ObtenerUniversidadPorIdQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (query.UniversidadAliadaId == Guid.Empty)
        {
            throw new ArgumentException("El identificador de la universidad aliada es obligatorio.", nameof(query));
        }

        var universidad = await _universidadRepository.ObtenerPorIdAsync(query.UniversidadAliadaId, cancellationToken)
            ?? throw new UniversidadAliadaNoEncontradaException(query.UniversidadAliadaId);

        return universidad.ADetalleDto();
    }
}
