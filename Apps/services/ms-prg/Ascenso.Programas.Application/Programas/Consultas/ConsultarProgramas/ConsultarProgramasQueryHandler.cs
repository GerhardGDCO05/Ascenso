using Ascenso.Programas.Application.Donantes.DTOs;
using Ascenso.Programas.Application.Programas.DTOs;
using Ascenso.Programas.Domain.Enums;
using Ascenso.Programas.Domain.Interfaces;

namespace Ascenso.Programas.Application.Programas.Consultas.ConsultarProgramas;

public sealed class ConsultarProgramasQueryHandler
{
    private readonly IProgramaBecaRepository _programaRepository;

    public ConsultarProgramasQueryHandler(IProgramaBecaRepository programaRepository)
    {
        _programaRepository = programaRepository ?? throw new ArgumentNullException(nameof(programaRepository));
    }

    public async Task<PaginadoResponse<ProgramaBecaResumenDto>> HandleAsync(
        ConsultarProgramasQuery query,
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

        EstadoProgramaBeca? estado = string.IsNullOrWhiteSpace(query.Estado)
            ? null
            : CodigoEnum.DesdeCodigo<EstadoProgramaBeca>(query.Estado);

        ModalidadCobertura? modalidad = string.IsNullOrWhiteSpace(query.ModalidadCobertura)
            ? null
            : CodigoEnum.DesdeCodigo<ModalidadCobertura>(query.ModalidadCobertura);

        var (items, totalCount) = await _programaRepository.ConsultarPaginadoAsync(
            query.Pagina,
            query.TamanoPagina,
            estado,
            query.PeriodoInicialId,
            modalidad,
            query.Busqueda,
            cancellationToken);

        return new PaginadoResponse<ProgramaBecaResumenDto>(
            items.Select(ProgramaBecaMapeador.AResumenDto).ToArray(),
            query.Pagina,
            query.TamanoPagina,
            totalCount);
    }
}
