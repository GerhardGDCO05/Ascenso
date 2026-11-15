namespace Ascenso.Becas.Application.Correlacion;

/// <summary>
/// Nombres y límites compartidos del protocolo de correlación.
/// </summary>
public static class CorrelacionConstantes
{
    public const string Encabezado = "X-Correlation-Id";

    public const string ItemHttp = "CorrelationId";

    public const int LongitudMaxima = 128;
}
