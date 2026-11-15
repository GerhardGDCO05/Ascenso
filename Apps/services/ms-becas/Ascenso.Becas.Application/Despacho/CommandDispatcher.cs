using Microsoft.Extensions.DependencyInjection;

namespace Ascenso.Becas.Application.Despacho;

/// <summary>
/// Despacha comandos validando primero la solicitud y resolviendo el handler
/// registrado por ensamblado.
/// </summary>
public sealed class CommandDispatcher : ICommandDispatcher
{
    private readonly IServiceProvider _proveedor;

    public CommandDispatcher(IServiceProvider proveedor)
    {
        ArgumentNullException.ThrowIfNull(proveedor);
        _proveedor = proveedor;
    }

    public async Task EnviarAsync<TCommand>(
        TCommand command,
        CancellationToken cancellationToken = default)
        where TCommand : ICommand
    {
        ArgumentNullException.ThrowIfNull(command);

        await ValidadorSolicitud.ValidarAsync(_proveedor, command, cancellationToken);

        var handler = _proveedor.GetService<ICommandHandler<TCommand>>()
            ?? throw new HandlerNoEncontradoException(typeof(TCommand));

        await handler.HandleAsync(command, cancellationToken);
    }

    public async Task<TResult> EnviarAsync<TCommand, TResult>(
        TCommand command,
        CancellationToken cancellationToken = default)
        where TCommand : ICommand<TResult>
    {
        ArgumentNullException.ThrowIfNull(command);

        await ValidadorSolicitud.ValidarAsync(_proveedor, command, cancellationToken);

        var handler = _proveedor.GetService<ICommandHandler<TCommand, TResult>>()
            ?? throw new HandlerNoEncontradoException(typeof(TCommand));

        return await handler.HandleAsync(command, cancellationToken);
    }
}
