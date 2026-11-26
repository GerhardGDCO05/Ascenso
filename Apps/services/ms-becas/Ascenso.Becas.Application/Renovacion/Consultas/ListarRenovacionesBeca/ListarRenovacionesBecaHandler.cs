using Ascenso.Becas.Application.Despacho;
using Ascenso.Becas.Application.Excepciones;
using Ascenso.Becas.Application.Renovacion.Dtos;
using Ascenso.Becas.Application.Renovacion.Puertos;

namespace Ascenso.Becas.Application.Renovacion.Consultas.ListarRenovacionesBeca;

public sealed class ListarRenovacionesBecaHandler
    : IQueryHandler<ListarRenovacionesBecaQuery, PaginaRenovacionesDto>
{
    private readonly IConsultasRenovacion _consultas;

    public ListarRenovacionesBecaHandler(IConsultasRenovacion consultas)
        => _consultas = consultas ?? throw new ArgumentNullException(nameof(consultas));

    public Task<PaginaRenovacionesDto> HandleAsync(
        ListarRenovacionesBecaQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        if (query.BecaId == Guid.Empty || query.Pagina < 1
            || query.Tamano is < 1 or > 50)
            throw new SolicitudInvalidaException(
                "La beca, la página y el tamaño (1 a 50) son obligatorios.");
        return _consultas.ListarPorBecaAsync(query.BecaId,
            query.Pagina, query.Tamano, cancellationToken);
    }
}
