using Ascenso.Programas.Application.Donantes.DTOs;
using Ascenso.Programas.Application.Universidades.DTOs;
using Ascenso.Programas.Domain.Enums;
using Ascenso.Programas.Domain.Interfaces;

namespace Ascenso.Programas.Application.Universidades.Consultas.ConsultarUniversidades;

public sealed class ConsultarUniversidadesQueryHandler
{
    private readonly IUniversidadAliadaRepository _universidadRepository;

    public ConsultarUniversidadesQueryHandler(IUniversidadAliadaRepository universidadRepository)
    {
        _universidadRepository = universidadRepository ?? throw new ArgumentNullException(nameof(universidadRepository));
    }

    public async Task<PaginadoResponse<UniversidadResumenDto>> HandleAsync(
        ConsultarUniversidadesQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (query.Pagina < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(query), "El número de página debe ser mayor o igual a 1.");
        }

        if (query.TamanoPagina is < 1 or > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(query), "El tamaño de página debe estar entre 1 y 100.");
        }

        EstadoUniversidad? estado = null;
        if (!string.IsNullOrWhiteSpace(query.Estado))
        {
            if (!DonanteMapeador.IntentarConvertirEnum<EstadoUniversidad>(query.Estado, out var estadoParseado))
            {
                throw new ArgumentException($"El estado de universidad '{query.Estado}' no es válido.", nameof(query));
            }

            estado = estadoParseado;
        }

        var (items, total) = await _universidadRepository.ConsultarPaginadoAsync(
            query.Pagina,
            query.TamanoPagina,
            estado,
            query.Busqueda,
            cancellationToken);

        return new PaginadoResponse<UniversidadResumenDto>(
            items.Select(u => u.AResumenDto()).ToList(),
            query.Pagina,
            query.TamanoPagina,
            total);
    }
}
