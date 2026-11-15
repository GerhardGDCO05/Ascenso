using Ascenso.Becas.Application.Despacho;
using Ascenso.Becas.Application.Excepciones;
using Ascenso.Becas.Application.Otorgamiento.Dtos;
using Ascenso.Becas.Application.Otorgamiento.Puertos;

namespace Ascenso.Becas.Application.Otorgamiento.Consultas.ObtenerOferta;

/// <summary>
/// Resuelve la oferta desde el modelo de lectura.
/// </summary>
public sealed class ObtenerOfertaHandler : IQueryHandler<ObtenerOfertaQuery, OfertaBecaDto>
{
    private readonly IConsultasBeca _consultas;

    public ObtenerOfertaHandler(IConsultasBeca consultas)
    {
        _consultas = consultas ?? throw new ArgumentNullException(nameof(consultas));
    }

    public async Task<OfertaBecaDto> HandleAsync(
        ObtenerOfertaQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        return await _consultas.ObtenerOfertaAsync(query.OfertaId, cancellationToken)
            ?? throw new RecursoNoEncontradoException("oferta", query.OfertaId);
    }
}
