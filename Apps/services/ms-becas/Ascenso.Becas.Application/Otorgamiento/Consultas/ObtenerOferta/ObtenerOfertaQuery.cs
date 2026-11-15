using Ascenso.Becas.Application.Despacho;
using Ascenso.Becas.Application.Otorgamiento.Dtos;

namespace Ascenso.Becas.Application.Otorgamiento.Consultas.ObtenerOferta;

/// <summary>
/// Consulta una oferta de beca por su identificador.
/// </summary>
public sealed record ObtenerOfertaQuery(Guid OfertaId) : IQuery<OfertaBecaDto>;
