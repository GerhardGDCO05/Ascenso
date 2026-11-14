using Ascenso.Programas.Application.Donantes.DTOs;
using Ascenso.Programas.Domain.Enums;
using Ascenso.Programas.Domain.Interfaces;
using FluentValidation;
using FluentValidation.Results;

namespace Ascenso.Programas.Application.Donantes.Consultas.ConsultarDonantes;

public sealed class ConsultarDonantesQueryHandler
{
    private readonly IDonanteRepository _donanteRepository;

    public ConsultarDonantesQueryHandler(IDonanteRepository donanteRepository)
    {
        _donanteRepository = donanteRepository ?? throw new ArgumentNullException(nameof(donanteRepository));
    }

    public async Task<PaginadoResponse<DonanteResumenDto>> HandleAsync(
        ConsultarDonantesQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var pagina = query.Pagina < 1 ? 1 : query.Pagina;
        var tamanoPagina = query.TamanoPagina switch
        {
            < 1 => 20,
            > 100 => 100,
            _ => query.TamanoPagina
        };

        TipoDonante? tipoFiltro = null;
        if (!string.IsNullOrWhiteSpace(query.TipoDonante))
        {
            if (!DonanteMapeador.IntentarConvertirEnum<TipoDonante>(query.TipoDonante, out var tipoParsed))
            {
                throw new ValidationException([
                    new ValidationFailure(nameof(query.TipoDonante), $"Tipo de donante '{query.TipoDonante}' no es válido.")
                ]);
            }

            tipoFiltro = tipoParsed;
        }

        EstadoDonante? estadoFiltro = null;
        if (!string.IsNullOrWhiteSpace(query.EstadoDonante))
        {
            if (!DonanteMapeador.IntentarConvertirEnum<EstadoDonante>(query.EstadoDonante, out var estadoParsed))
            {
                throw new ValidationException([
                    new ValidationFailure(nameof(query.EstadoDonante), $"Estado de donante '{query.EstadoDonante}' no es válido.")
                ]);
            }

            estadoFiltro = estadoParsed;
        }

        var (items, total) = await _donanteRepository.ConsultarPaginadoAsync(
            pagina,
            tamanoPagina,
            tipoFiltro,
            estadoFiltro,
            query.Busqueda,
            cancellationToken);

        var resumenes = items.Select(donante => donante.AResumenDto()).ToList();

        return new PaginadoResponse<DonanteResumenDto>(resumenes, pagina, tamanoPagina, total);
    }
}

