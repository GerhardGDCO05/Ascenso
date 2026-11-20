using Ascenso.Postulaciones.Domain.Postulantes.Agregados;

namespace Ascenso.Postulaciones.Domain.Interfaces;

/// <summary>
/// Puerto de persistencia del agregado <see cref="Postulante"/>.
/// </summary>
public interface IPostulanteRepository
{
    Task<Postulante?> ObtenerPorIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Postulante?> ObtenerPorUsuarioIdAsync(Guid usuarioId, CancellationToken cancellationToken = default);

    Task<Postulante?> ObtenerParaActualizarPorUsuarioIdAsync(
        Guid usuarioId,
        CancellationToken cancellationToken = default);

    Task<bool> ExistePorNumeroDocumentoAsync(
        string numeroDocumentoIdentidad,
        CancellationToken cancellationToken = default);

    Task AgregarAsync(Postulante postulante, CancellationToken cancellationToken = default);
}
