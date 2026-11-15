namespace Ascenso.Becas.Application.Despacho;

/// <summary>
/// Marca un caso de uso de escritura sin resultado.
/// </summary>
public interface ICommand
{
}

/// <summary>
/// Marca un caso de uso de escritura que devuelve un resultado.
/// </summary>
/// <typeparam name="TResult">Tipo del resultado del caso de uso.</typeparam>
public interface ICommand<TResult> : ICommand
{
}
