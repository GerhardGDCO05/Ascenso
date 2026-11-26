using Ascenso.Becas.Application.Despacho;
using Ascenso.Becas.Application.SeguimientoAcademico.Dtos;

namespace Ascenso.Becas.Application.SeguimientoAcademico.Comandos.EvaluarPermanencia;

public sealed record EvaluarPermanenciaCommand(Guid ReporteAcademicoId)
    : ICommand<EvaluacionPermanenciaDto>;
