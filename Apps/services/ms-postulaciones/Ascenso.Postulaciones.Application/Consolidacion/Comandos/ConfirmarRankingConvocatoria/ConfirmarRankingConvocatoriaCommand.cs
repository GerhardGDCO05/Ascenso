namespace Ascenso.Postulaciones.Application.Consolidacion.Comandos.ConfirmarRankingConvocatoria;

public sealed record class ConfirmarRankingConvocatoriaCommand(
    Guid ConvocatoriaId,
    bool ConfirmarConAdvertenciaDiscrepancias = false);
