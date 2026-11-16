using System.Text.Json;
using System.Text.Json.Serialization;

namespace Ascenso.Becas.Infrastructure.Clientes;

/// <summary>
/// Obtiene y cachea el <em>Service Token</em> de Keycloak usando
/// <em>client credentials</em>. La renovación ocurre antes de que expire el
/// token y una única solicitud concurrente actualiza la caché.
/// </summary>
public sealed class ServiceTokenProvider : IDisposable
{
    private static readonly JsonSerializerOptions _opcionesJson =
        new(JsonSerializerDefaults.Web);

    private readonly HttpClient _cliente;
    private readonly ServiceTokenOptions _opciones;
    private readonly SemaphoreSlim _candado = new(1, 1);
    private string? _token;
    private long _expiraEnTicks;

    public ServiceTokenProvider(HttpClient cliente, ServiceTokenOptions opciones)
    {
        _cliente = cliente ?? throw new ArgumentNullException(nameof(cliente));
        _opciones = opciones ?? throw new ArgumentNullException(nameof(opciones));
    }

    public async Task<string> ObtenerTokenAsync(
        CancellationToken cancellationToken = default)
    {
        if (TieneTokenVigente())
        {
            return Volatile.Read(ref _token)!;
        }

        await _candado.WaitAsync(cancellationToken);

        try
        {
            if (TieneTokenVigente())
            {
                return Volatile.Read(ref _token)!;
            }

            return await SolicitarTokenAsync(cancellationToken);
        }
        finally
        {
            _candado.Release();
        }
    }

    /// <summary>
    /// Descarta el token actual para que la siguiente llamada solicite uno
    /// nuevo. Se invoca cuando el proveedor responde <c>401</c>.
    /// </summary>
    public void Invalidar()
    {
        Volatile.Write(ref _token, null);
        Volatile.Write(ref _expiraEnTicks, 0);
    }

    public void Dispose()
    {
        _candado.Dispose();
        _cliente.Dispose();
    }

    private bool TieneTokenVigente()
    {
        var token = Volatile.Read(ref _token);

        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        var expiraEn = new DateTimeOffset(
            Volatile.Read(ref _expiraEnTicks),
            TimeSpan.Zero);
        var margen = TimeSpan.FromSeconds(Math.Max(0, _opciones.MargenRenovacionSegundos));

        return DateTimeOffset.UtcNow < expiraEn - margen;
    }

    private async Task<string> SolicitarTokenAsync(CancellationToken cancellationToken)
    {
        if (!Uri.TryCreate(_opciones.TokenEndpoint, UriKind.Absolute, out var endpoint))
        {
            throw new HttpRequestException(
                "El endpoint de Service Token de Keycloak no está configurado correctamente.");
        }

        if (string.IsNullOrWhiteSpace(_opciones.ClientId)
            || string.IsNullOrWhiteSpace(_opciones.ClientSecret))
        {
            throw new HttpRequestException(
                "Las credenciales de Service Token de Keycloak no están configuradas.");
        }

        using var contenido = new FormUrlEncodedContent(
        [
            new KeyValuePair<string, string>("grant_type", "client_credentials"),
            new KeyValuePair<string, string>("client_id", _opciones.ClientId),
            new KeyValuePair<string, string>("client_secret", _opciones.ClientSecret)
        ]);

        using var respuesta = await _cliente.PostAsync(endpoint, contenido, cancellationToken);
        var cuerpo = await respuesta.Content.ReadAsStringAsync(cancellationToken);

        if (!respuesta.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"Keycloak rechazó la solicitud de Service Token con estatus {(int)respuesta.StatusCode}.",
                inner: null,
                respuesta.StatusCode);
        }

        RespuestaToken? resultado;

        try
        {
            resultado = JsonSerializer.Deserialize<RespuestaToken>(cuerpo, _opcionesJson);
        }
        catch (JsonException excepcion)
        {
            throw new HttpRequestException(
                "Keycloak devolvió una respuesta de Service Token inválida.",
                excepcion);
        }

        if (resultado is null
            || string.IsNullOrWhiteSpace(resultado.AccessToken)
            || resultado.ExpiresIn <= 0)
        {
            throw new HttpRequestException(
                "Keycloak devolvió un Service Token sin datos de vigencia válidos.");
        }

        var expiraEn = DateTimeOffset.UtcNow.AddSeconds(resultado.ExpiresIn);

        Volatile.Write(ref _token, resultado.AccessToken);
        Volatile.Write(ref _expiraEnTicks, expiraEn.UtcTicks);

        return resultado.AccessToken;
    }

    private sealed record RespuestaToken
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; init; } = string.Empty;

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; init; }

        [JsonPropertyName("token_type")]
        public string TokenType { get; init; } = string.Empty;
    }
}
