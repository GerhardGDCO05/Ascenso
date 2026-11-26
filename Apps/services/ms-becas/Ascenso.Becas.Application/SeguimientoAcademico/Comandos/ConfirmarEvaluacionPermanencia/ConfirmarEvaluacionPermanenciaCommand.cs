using Ascenso.Becas.Application.Despacho;
using Ascenso.Becas.Application.SeguimientoAcademico.Dtos;

namespace Ascenso.Becas.Application.SeguimientoAcademico.Comandos.ConfirmarEvaluacionPermanencia;

public sealed record ConfirmarEvaluacionPermanenciaCommand(
    Guid ReporteAcademicoId, bool Confirmar, string MotivoDecision,
    string SiguientePasoBecario) : ICommand<EvaluacionPermanenciaDto>;
