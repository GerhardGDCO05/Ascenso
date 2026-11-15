using Microsoft.Extensions.DependencyInjection;

namespace Ascenso.Becas.Application.Despacho;

/// <summary>
/// Despacha consultas validando primero la solicitud y resolviendo el handler
/// registrado por ensamblado.
/// </summary>
public sealed class QueryDispatcher : IQueryDispatcher
{
    private readonly IServiceProvider _proveedor;

    public QueryDispatcher(IServiceProvider proveedor)
    {
        ArgumentNullException.ThrowIfNull(proveedor);
        _proveedor = proveedor;
    }

    public async Task<TResult> ConsultarAsync<TQuery, TResult>(
        TQuery query,
        CancellationToken cancellationToken = default)
        where TQuery : IQuery<TResult>
    {
        ArgumentNullException.ThrowIfNull(query);

        await ValidadorSolicitud.ValidarAsync(_proveedor, query, cancellationToken);

        var handler = _proveedor.GetService<IQueryHandler<TQuery, TResult>>()
            ?? throw new HandlerNoEncontradoException(typeof(TQuery));

        return await handler.HandleAsync(query, cancellationToken);
    }
}
