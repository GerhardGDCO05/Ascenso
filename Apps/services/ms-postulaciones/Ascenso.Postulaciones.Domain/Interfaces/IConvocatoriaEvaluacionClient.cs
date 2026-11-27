using Ascenso.Postulaciones.Domain.Evaluaciones.ObjetosValor;

namespace Ascenso.Postulaciones.Domain.Interfaces;

/// <summary>
/// Puerto ACL hacia MS-PRG para consultar la ventana formal de revisión de una convocatoria.
/// </summary>
public interface IConvocatoriaEvaluacionClient
{
    Task<VentanaRevisionConvocatoria?> ObtenerVentanaRevisionAsync(
        Guid convocatoriaId,
        CancellationToken cancellationToken = default);
}
