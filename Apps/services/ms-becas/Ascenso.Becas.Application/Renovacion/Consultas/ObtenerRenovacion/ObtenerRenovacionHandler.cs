using Ascenso.Becas.Application.Despacho;
using Ascenso.Becas.Application.Excepciones;
using Ascenso.Becas.Application.Renovacion.Dtos;
using Ascenso.Becas.Application.Renovacion.Puertos;

namespace Ascenso.Becas.Application.Renovacion.Consultas.ObtenerRenovacion;

public sealed class ObtenerRenovacionHandler
    : IQueryHandler<ObtenerRenovacionQuery, SolicitudRenovacionDto>
{
    private readonly IConsultasRenovacion _consultas;

    public ObtenerRenovacionHandler(IConsultasRenovacion consultas)
        => _consultas = consultas ?? throw new ArgumentNullException(nameof(consultas));

    public async Task<SolicitudRenovacionDto> HandleAsync(
        ObtenerRenovacionQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        return await _consultas.ObtenerAsync(query.SolicitudId, cancellationToken)
            ?? throw new RecursoNoEncontradoException(
                "solicitud de renovación", query.SolicitudId);
    }
}
