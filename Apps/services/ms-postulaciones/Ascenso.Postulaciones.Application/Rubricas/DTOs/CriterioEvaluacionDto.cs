namespace Ascenso.Postulaciones.Application.Rubricas.DTOs;

public sealed record class CriterioEvaluacionDto(
    Guid Id,
    string Nombre,
    string Descripcion,
    decimal PesoPorcentual,
    decimal PuntajeMinimo,
    decimal PuntajeMaximo,
    int Orden);
