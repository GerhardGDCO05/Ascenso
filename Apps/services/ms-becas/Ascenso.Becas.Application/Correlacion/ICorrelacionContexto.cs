namespace Ascenso.Becas.Application.Correlacion;

/// <summary>
/// Contexto de correlación de la operación actual, disponible también para
/// trabajos en segundo plano y eventos de integración.
/// </summary>
public interface ICorrelacionContexto
{
    string CorrelationId { get; }
}
