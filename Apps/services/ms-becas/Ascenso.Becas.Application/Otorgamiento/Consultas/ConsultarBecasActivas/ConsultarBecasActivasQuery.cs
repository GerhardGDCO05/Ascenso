using Ascenso.Becas.Application.Despacho;
using Ascenso.Becas.Application.Otorgamiento.Dtos;

namespace Ascenso.Becas.Application.Otorgamiento.Consultas.ConsultarBecasActivas;

/// <summary>
/// Consulta interna de becas activas de un estudiante en un periodo.
/// </summary>
public sealed record ConsultarBecasActivasQuery(
    Guid PostulanteEstudianteId,
    Guid PeriodoAcademicoId)
    : IQuery<IReadOnlyList<BecaActivaDto>>;
