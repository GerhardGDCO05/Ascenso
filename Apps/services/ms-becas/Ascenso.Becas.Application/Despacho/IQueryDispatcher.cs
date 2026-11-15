namespace Ascenso.Becas.Application.Despacho;

/// <summary>
/// Punto de entrada de las consultas de lectura.
/// </summary>
public interface IQueryDispatcher
{
    Task<TResult> ConsultarAsync<TQuery, TResult>(
        TQuery query,
        CancellationToken cancellationToken = default)
        where TQuery : IQuery<TResult>;
}
