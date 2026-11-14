namespace Ascenso.Programas.Domain.Interfaces;

/// <summary>
/// Unidad de trabajo transaccional para confirmar cambios en el agregado de programas.
/// </summary>
public interface IUnitOfWork
{
    Task<int> GuardarCambiosAsync(CancellationToken cancellationToken = default);
}

