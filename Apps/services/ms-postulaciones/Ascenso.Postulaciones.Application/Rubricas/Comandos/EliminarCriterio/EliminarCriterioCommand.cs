namespace Ascenso.Postulaciones.Application.Rubricas.Comandos.EliminarCriterio;

public sealed record class EliminarCriterioCommand(
    Guid RubricaId,
    Guid CriterioId);
