namespace Ascenso.Becas.Application.Despacho;

/// <summary>
/// Ejecuta una consulta de lectura.
/// </summary>
public interface IQueryHandler<in TQuery, TResult>
    where TQuery : IQuery<TResult>
{
    Task<TResult> HandleAsync(TQuery query, CancellationToken cancellationToken = default);
}
