using Ascenso.Becas.Domain.Otorgamiento.Agregados;

namespace Ascenso.Becas.Domain.Ports;

/// <summary>
/// Puerto de persistencia del agregado <c>Beca</c>.
/// </summary>
public interface IBecaRepository
{
    /// <summary>
    /// Recupera el agregado completo, incluyendo la oferta, el compromiso y el
    /// historial de estados.
    /// </summary>
    Task<Beca?> ObtenerPorIdAsync(Guid becaId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Agrega una nueva raíz de agregado al contexto de persistencia.
    /// </summary>
    Task AgregarAsync(Beca beca, CancellationToken cancellationToken = default);

    /// <summary>
    /// Bloquea la fila de la beca con <c>SELECT ... FOR UPDATE</c> y devuelve el
    /// agregado completo. Requiere una transacción activa.
    /// </summary>
    Task<Beca?> ObtenerBecaParaActualizarAsync(
        Guid becaId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adquiere el bloqueo transaccional por estudiante con
    /// <c>pg_advisory_xact_lock</c>. Requiere una transacción activa.
    /// </summary>
    Task AdquirirBloqueoEstudianteAsync(
        Guid estudianteId,
        CancellationToken cancellationToken = default);

    Task<bool> ExisteAsync(Guid becaId, CancellationToken cancellationToken = default);

    Task<bool> ExisteActivaConCoberturaTotalAsync(
        Guid estudianteId,
        CancellationToken cancellationToken = default);

    Task<bool> ExisteActivaEnPeriodoAsync(
        Guid estudianteId,
        Guid periodoAcademicoId,
        CancellationToken cancellationToken = default);
}
