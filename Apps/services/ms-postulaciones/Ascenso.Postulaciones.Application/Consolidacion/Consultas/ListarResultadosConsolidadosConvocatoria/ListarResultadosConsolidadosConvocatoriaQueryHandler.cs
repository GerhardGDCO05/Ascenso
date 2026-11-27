using Ascenso.Postulaciones.Application.Asignaciones.DTOs;
using Ascenso.Postulaciones.Application.Consolidacion.DTOs;
using Ascenso.Postulaciones.Domain.Interfaces;
using FluentValidation;

namespace Ascenso.Postulaciones.Application.Consolidacion.Consultas.ListarResultadosConsolidadosConvocatoria;

public sealed class ListarResultadosConsolidadosConvocatoriaQueryHandler
{
    private readonly IResultadoConsolidadoRepository _resultadoRepository;
    private readonly IValidator<ListarResultadosConsolidadosConvocatoriaQuery> _validator;

    public ListarResultadosConsolidadosConvocatoriaQueryHandler(
        IResultadoConsolidadoRepository resultadoRepository,
        IValidator<ListarResultadosConsolidadosConvocatoriaQuery> validator)
    {
        _resultadoRepository = resultadoRepository ?? throw new ArgumentNullException(nameof(resultadoRepository));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
    }

    public async Task<PaginaDto<ResultadoConsolidadoDto>> HandleAsync(
        ListarResultadosConsolidadosConvocatoriaQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        await _validator.ValidateAndThrowAsync(query, cancellationToken);

        var pagina = await _resultadoRepository.ListarPorConvocatoriaAsync(
            query.ConvocatoriaId,
            query.AlertaDiscrepancia,
            query.DiscrepanciaRevisada,
            query.Pagina,
            query.Tamano,
            cancellationToken);

        var items = pagina.Items
            .Select(resultado => resultado.ADto())
            .ToArray();

        return new PaginaDto<ResultadoConsolidadoDto>(
            items,
            query.Pagina,
            query.Tamano,
            pagina.Total);
    }
}
