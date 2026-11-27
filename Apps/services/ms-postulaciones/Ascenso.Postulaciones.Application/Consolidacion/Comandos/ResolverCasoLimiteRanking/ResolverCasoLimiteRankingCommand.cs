namespace Ascenso.Postulaciones.Application.Consolidacion.Comandos.ResolverCasoLimiteRanking;

public sealed record class ResolverCasoLimiteRankingCommand(
    Guid ConvocatoriaId,
    IReadOnlyList<Guid> PostulacionesSeleccionadasIds,
    string Justificacion);
