namespace Ascenso.Postulaciones.Domain.Interfaces;

/// <summary>
/// Unidad de trabajo transaccional para confirmar cambios en el agregado de postulaciones.
/// </summary>
public interface IUnitOfWork
{
    Task<int> GuardarCambiosAsync(CancellationToken cancellationToken = default);
}

