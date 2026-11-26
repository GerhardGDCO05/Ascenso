namespace Ascenso.Postulaciones.Application.Rubricas.Comandos.AgregarCriterio;

public sealed record class AgregarCriterioCommand(
    Guid RubricaId,
    string Nombre,
    string Descripcion,
    decimal PesoPorcentual,
    decimal PuntajeMinimo,
    decimal PuntajeMaximo);
