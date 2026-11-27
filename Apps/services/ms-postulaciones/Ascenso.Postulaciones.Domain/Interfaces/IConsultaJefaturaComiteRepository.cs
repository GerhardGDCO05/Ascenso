namespace Ascenso.Postulaciones.Domain.Interfaces;

/// <summary>
/// Puerto de solo lectura para verificar si un usuario autenticado ejerce la jefatura activa
/// del comité evaluador correspondiente a una convocatoria.
/// </summary>
public interface IConsultaJefaturaComiteRepository
{
    Task<bool> EsJefaturaActivaEnConvocatoriaAsync(
        Guid convocatoriaId,
        Guid usuarioId,
        CancellationToken cancellationToken = default);
}
