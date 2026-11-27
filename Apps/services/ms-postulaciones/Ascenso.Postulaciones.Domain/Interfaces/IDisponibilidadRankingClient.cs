using Ascenso.Postulaciones.Domain.Consolidacion.ObjetosValor;

namespace Ascenso.Postulaciones.Domain.Interfaces;

/// <summary>
/// Puerto ACL fail-closed hacia MS-PRG para consultar la disponibilidad de cupos y presupuesto de una convocatoria.
/// </summary>
public interface IDisponibilidadRankingClient
{
    Task<DisponibilidadProgramaRanking?> ObtenerDisponibilidadPorConvocatoriaAsync(
        Guid convocatoriaId,
        CancellationToken cancellationToken = default);
}
