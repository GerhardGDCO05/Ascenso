using Ascenso.Programas.Application.Donantes.DTOs;
using Ascenso.Programas.Domain.Enums;
using Ascenso.Programas.Domain.Interfaces;

namespace Ascenso.Programas.Application.Convocatorias.Consultas.ConsultarConvocatorias;

public sealed class ConsultarConvocatoriasQueryHandler
{
    private readonly IConvocatoriaRepository _convocatoriaRepository;

    public ConsultarConvocatoriasQueryHandler(IConvocatoriaRepository convocatoriaRepository)
    {
        _convocatoriaRepository = convocatoriaRepository ?? throw new ArgumentNullException(nameof(convocatoriaRepository));
    }

    public async Task<PaginadoResponse<DTOs.ConvocatoriaResumenDto>> HandleAsync(
        ConsultarConvocatoriasQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (query.Pagina < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(query), "La página debe ser mayor o igual a 1.");
        }

        if (query.TamanoPagina is < 1 or > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(query), "El tamaño de página debe estar entre 1 y 100.");
        }

        EstadoConvocatoria? estado = string.IsNullOrWhiteSpace(query.Estado)
            ? null
            : CodigoEnum.DesdeCodigo<EstadoConvocatoria>(query.Estado);

        var (items, totalCount) = await _convocatoriaRepository.ConsultarPaginadoAsync(
            query.Pagina,
            query.TamanoPagina,
            estado,
            query.ProgramaBecaId,
            query.Busqueda,
            cancellationToken);

        return new PaginadoResponse<DTOs.ConvocatoriaResumenDto>(
            items.Select(ConvocatoriaMapeador.AResumenDto).ToArray(),
            query.Pagina,
            query.TamanoPagina,
            totalCount);
    }
}
