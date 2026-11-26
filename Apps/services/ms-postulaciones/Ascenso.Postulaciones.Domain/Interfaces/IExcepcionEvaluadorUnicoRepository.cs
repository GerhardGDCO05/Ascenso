using Ascenso.Postulaciones.Domain.Asignaciones.Entidades;

namespace Ascenso.Postulaciones.Domain.Interfaces;

/// <summary>
/// Puerto de persistencia append-only de <see cref="AutorizacionExcepcionEvaluadorUnico"/>.
/// </summary>
public interface IExcepcionEvaluadorUnicoRepository
{
    Task<AutorizacionExcepcionEvaluadorUnico?> ObtenerPorPostulacionAsync(
        Guid postulacionId,
        CancellationToken cancellationToken = default);

    Task AgregarAsync(
        AutorizacionExcepcionEvaluadorUnico autorizacion,
        CancellationToken cancellationToken = default);
}
