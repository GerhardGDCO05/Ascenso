using System.Net;
using System.Net.Http.Headers;

namespace Ascenso.Becas.Infrastructure.Clientes;

/// <summary>
/// Agrega el <c>Authorization: Bearer</c> con el <em>Service Token</em> a las
/// llamadas internas. Si el proveedor responde <c>401</c>, invalida el token
/// para que la siguiente operación solicite uno nuevo.
/// </summary>
public sealed class TokenServicioDelegatingHandler : DelegatingHandler
{
    private readonly ServiceTokenProvider _proveedor;

    public TokenServicioDelegatingHandler(ServiceTokenProvider proveedor)
    {
        _proveedor = proveedor ?? throw new ArgumentNullException(nameof(proveedor));
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var token = await _proveedor.ObtenerTokenAsync(cancellationToken);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var respuesta = await base.SendAsync(request, cancellationToken);

        if (respuesta.StatusCode == HttpStatusCode.Unauthorized)
        {
            _proveedor.Invalidar();
        }

        return respuesta;
    }
}
