using Ascenso.Becas.Application.Despacho;
using Ascenso.Becas.Application.SeguimientoAcademico.Dtos;

namespace Ascenso.Becas.Application.SeguimientoAcademico.Comandos.AplicarConsecuenciaPermanencia;

public sealed record AplicarConsecuenciaPermanenciaCommand(Guid ReporteAcademicoId)
    : ICommand<EvaluacionPermanenciaDto>;
