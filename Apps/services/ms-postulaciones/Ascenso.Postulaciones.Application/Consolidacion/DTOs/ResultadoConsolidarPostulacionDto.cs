namespace Ascenso.Postulaciones.Application.Consolidacion.DTOs;

public sealed record class ResultadoConsolidarPostulacionDto(
    ResultadoConsolidadoDto Resultado,
    bool EsCreacion);
