using Ascenso.Becas.Application.Correlacion;

namespace Ascenso.Becas.Infrastructure.Clientes;

/// <summary>
/// Propaga <c>X-Correlation-Id</c> a las llamadas HTTP salientes usando el
/// contexto de correlación de la aplicación.
/// </summary>
public sealed class CorrelacionDelegatingHandler : DelegatingHandler
{
    private readonly ICorrelacionContexto _correlacion;

    public CorrelacionDelegatingHandler(ICorrelacionContexto correlacion)
    {
        _correlacion = correlacion ?? throw new ArgumentNullException(nameof(correlacion));
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var correlacion = _correlacion.CorrelationId;
        if (!string.IsNullOrWhiteSpace(correlacion))
        {
            request.Headers.Remove(CorrelacionConstantes.Encabezado);
            request.Headers.TryAddWithoutValidation(CorrelacionConstantes.Encabezado, correlacion);
        }

        return base.SendAsync(request, cancellationToken);
    }
}
