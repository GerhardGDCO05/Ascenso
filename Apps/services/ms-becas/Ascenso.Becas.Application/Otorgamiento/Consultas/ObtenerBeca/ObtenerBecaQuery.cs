using Ascenso.Becas.Application.Despacho;
using Ascenso.Becas.Application.Otorgamiento.Dtos;

namespace Ascenso.Becas.Application.Otorgamiento.Consultas.ObtenerBeca;

/// <summary>
/// Consulta una beca por su identificador.
/// </summary>
public sealed record ObtenerBecaQuery(Guid BecaId) : IQuery<BecaDto>;
