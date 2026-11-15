namespace Ascenso.Becas.Application.Despacho;

/// <summary>
/// Punto de entrada de los casos de uso de escritura.
/// </summary>
public interface ICommandDispatcher
{
    Task EnviarAsync<TCommand>(
        TCommand command,
        CancellationToken cancellationToken = default)
        where TCommand : ICommand;

    Task<TResult> EnviarAsync<TCommand, TResult>(
        TCommand command,
        CancellationToken cancellationToken = default)
        where TCommand : ICommand<TResult>;
}
