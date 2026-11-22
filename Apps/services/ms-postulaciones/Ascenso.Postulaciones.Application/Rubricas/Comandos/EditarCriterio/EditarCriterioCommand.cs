namespace Ascenso.Postulaciones.Application.Rubricas.Comandos.EditarCriterio;

public sealed record class EditarCriterioCommand(
    Guid RubricaId,
    Guid CriterioId,
    string Nombre,
    string Descripcion,
    decimal PesoPorcentual,
    decimal PuntajeMinimo,
    decimal PuntajeMaximo);
