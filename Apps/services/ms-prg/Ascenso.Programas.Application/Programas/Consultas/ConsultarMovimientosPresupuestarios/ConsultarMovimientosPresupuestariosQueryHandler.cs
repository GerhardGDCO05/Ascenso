using Ascenso.Programas.Application.Donantes.DTOs;
using Ascenso.Programas.Application.Programas.DTOs;
using Ascenso.Programas.Domain.Enums;
using Ascenso.Programas.Domain.Excepciones;
using Ascenso.Programas.Domain.Interfaces;

namespace Ascenso.Programas.Application.Programas.Consultas.ConsultarMovimientosPresupuestarios;

public sealed class ConsultarMovimientosPresupuestariosQueryHandler
{
    private readonly IProgramaBecaRepository _programaRepository;

    public ConsultarMovimientosPresupuestariosQueryHandler(IProgramaBecaRepository programaRepository)
    {
        _programaRepository = programaRepository ?? throw new ArgumentNullException(nameof(programaRepository));
    }

    public async Task<PaginadoResponse<MovimientoPresupuestarioResponse>> HandleAsync(
        ConsultarMovimientosPresupuestariosQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (query.ProgramaBecaId == Guid.Empty)
        {
            throw new ArgumentException("El identificador del programa de becas es obligatorio.", nameof(query));
        }

        if (query.Pagina < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(query), "La página debe ser mayor o igual a 1.");
        }

        if (query.TamanoPagina is < 1 or > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(query), "El tamaño de página debe estar entre 1 y 100.");
        }

        _ = await _programaRepository.ObtenerPorIdAsync(query.ProgramaBecaId, cancellationToken)
            ?? throw new ProgramaBecaNoEncontradoException(query.ProgramaBecaId);

        TipoMovimientoPresupuestario? tipoMovimiento = string.IsNullOrWhiteSpace(query.TipoMovimiento)
            ? null
            : CodigoEnum.DesdeCodigo<TipoMovimientoPresupuestario>(query.TipoMovimiento);

        var (items, totalCount) = await _programaRepository.ConsultarMovimientosPaginadosAsync(
            query.ProgramaBecaId,
            query.Pagina,
            query.TamanoPagina,
            tipoMovimiento,
            query.PeriodoAcademicoId,
            query.ReferenciaExternaId,
            cancellationToken);

        return new PaginadoResponse<MovimientoPresupuestarioResponse>(
            items.Select(ProgramaBecaMapeador.AMovimientoResponse).ToArray(),
            query.Pagina,
            query.TamanoPagina,
            totalCount);
    }
}
