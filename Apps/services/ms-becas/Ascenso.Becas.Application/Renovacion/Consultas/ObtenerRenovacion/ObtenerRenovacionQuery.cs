using Ascenso.Becas.Application.Despacho;
using Ascenso.Becas.Application.Renovacion.Dtos;

namespace Ascenso.Becas.Application.Renovacion.Consultas.ObtenerRenovacion;

public sealed record ObtenerRenovacionQuery(Guid SolicitudId)
    : IQuery<SolicitudRenovacionDto>;
