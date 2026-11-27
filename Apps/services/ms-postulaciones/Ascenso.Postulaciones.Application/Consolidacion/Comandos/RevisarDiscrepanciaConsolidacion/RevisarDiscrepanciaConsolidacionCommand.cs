namespace Ascenso.Postulaciones.Application.Consolidacion.Comandos.RevisarDiscrepanciaConsolidacion;

public sealed record class RevisarDiscrepanciaConsolidacionCommand(
    Guid PostulacionId,
    string Comentario);
