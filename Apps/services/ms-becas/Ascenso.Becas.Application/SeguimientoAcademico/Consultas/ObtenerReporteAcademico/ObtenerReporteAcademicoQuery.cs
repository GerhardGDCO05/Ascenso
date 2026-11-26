using Ascenso.Becas.Application.Despacho;
using Ascenso.Becas.Application.SeguimientoAcademico.Dtos;

namespace Ascenso.Becas.Application.SeguimientoAcademico.Consultas.ObtenerReporteAcademico;

public sealed record ObtenerReporteAcademicoQuery(Guid ReporteAcademicoId)
    : IQuery<ReporteAcademicoDto>;
