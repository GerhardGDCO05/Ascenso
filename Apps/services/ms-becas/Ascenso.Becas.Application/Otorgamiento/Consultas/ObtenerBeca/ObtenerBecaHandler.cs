using Ascenso.Becas.Application.Despacho;
using Ascenso.Becas.Application.Excepciones;
using Ascenso.Becas.Application.Otorgamiento.Dtos;
using Ascenso.Becas.Application.Otorgamiento.Puertos;

namespace Ascenso.Becas.Application.Otorgamiento.Consultas.ObtenerBeca;

/// <summary>
/// Resuelve la beca desde el modelo de lectura.
/// </summary>
public sealed class ObtenerBecaHandler : IQueryHandler<ObtenerBecaQuery, BecaDto>
{
    private readonly IConsultasBeca _consultas;

    public ObtenerBecaHandler(IConsultasBeca consultas)
    {
        _consultas = consultas ?? throw new ArgumentNullException(nameof(consultas));
    }

    public async Task<BecaDto> HandleAsync(
        ObtenerBecaQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        return await _consultas.ObtenerBecaAsync(query.BecaId, cancellationToken)
            ?? throw new RecursoNoEncontradoException("beca", query.BecaId);
    }
}
