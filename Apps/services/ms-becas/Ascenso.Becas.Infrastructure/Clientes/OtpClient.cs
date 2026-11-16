using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using Ascenso.Becas.Application.Correlacion;
using Ascenso.Becas.Domain.Enums;
using Ascenso.Becas.Domain.Ports;

namespace Ascenso.Becas.Infrastructure.Clientes;

/// <summary>
/// Cliente de <c>MS-PLT</c> para consumir códigos OTP. No se invoca todavía
/// desde los casos de uso de este cambio; queda disponible para desembolsos,
/// revocaciones y modificaciones de datos de pago.
/// </summary>
public sealed class OtpClient : IOtpClient
{
    private readonly HttpClient _cliente;
    private readonly ICorrelacionContexto _correlacion;

    public OtpClient(
        HttpClient cliente,
        ICorrelacionContexto correlacion)
    {
        _cliente = cliente ?? throw new ArgumentNullException(nameof(cliente));
        _correlacion = correlacion ?? throw new ArgumentNullException(nameof(correlacion));
    }

    public async Task<ResultadoConsumoOtp> ConsumirAsync(
        Guid usuarioId,
        TipoOperacionOTP operacion,
        Guid recursoId,
        string huellaPayload,
        string codigo,
        CancellationToken cancellationToken = default)
    {
        if (usuarioId == Guid.Empty)
        {
            throw new ArgumentException(
                "El identificador del usuario es obligatorio.",
                nameof(usuarioId));
        }

        if (recursoId == Guid.Empty)
        {
            throw new ArgumentException(
                "El identificador del recurso es obligatorio.",
                nameof(recursoId));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(huellaPayload);
        ArgumentException.ThrowIfNullOrWhiteSpace(codigo);

        using var peticion = SolicitudIntegracion.Crear(
            HttpMethod.Post,
            "internal/v1/otp/consumir",
            _correlacion);

        peticion.Headers.TryAddWithoutValidation(
            ProgramasClient.EncabezadoIdempotencia,
            $"otp-{usuarioId:N}-{operacion.ACodigo()}-{recursoId:N}");

        peticion.Content = SolicitudIntegracion.ContenidoJson(new
        {
            usuarioId,
            operacion = operacion.ACodigo(),
            recursoId,
            huellaPayload,
            codigo
        });

        using var respuesta = await _cliente.SendAsync(
            peticion,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        if (respuesta.StatusCode == HttpStatusCode.NoContent)
        {
            return new ResultadoConsumoOtp(true);
        }

        var cuerpo = await SolicitudIntegracion.LeerCuerpoAsync(
            respuesta,
            cancellationToken);

        if (respuesta.IsSuccessStatusCode)
        {
            var resultado = LeerRespuesta(cuerpo);

            return resultado is null or { Consumido: true }
                ? new ResultadoConsumoOtp(true)
                : new ResultadoConsumoOtp(
                    false,
                    string.IsNullOrWhiteSpace(resultado.CodigoError)
                        ? "OTP_INVALIDO"
                        : resultado.CodigoError);
        }

        if ((int)respuesta.StatusCode is >= 400 and < 500)
        {
            var error = LeerError(cuerpo);
            var codigoError = error.Codigo
                ?? CodigoPredeterminado(respuesta.StatusCode);

            return new ResultadoConsumoOtp(false, codigoError);
        }

        throw new HttpRequestException(
            $"MS-PLT respondió {(int)respuesta.StatusCode} al consumir el OTP.",
            inner: null,
            respuesta.StatusCode);
    }

    private static RespuestaConsumo? LeerRespuesta(string cuerpo)
    {
        if (string.IsNullOrWhiteSpace(cuerpo))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<RespuestaConsumo>(
                cuerpo,
                SolicitudIntegracion.OpcionesJson);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static ErrorIntegracion LeerError(string cuerpo)
    {
        if (string.IsNullOrWhiteSpace(cuerpo))
        {
            return new ErrorIntegracion(null);
        }

        try
        {
            var error = JsonSerializer.Deserialize<RespuestaError>(
                cuerpo,
                SolicitudIntegracion.OpcionesJson);

            return new ErrorIntegracion(error?.Codigo ?? error?.Code);
        }
        catch (JsonException)
        {
            return new ErrorIntegracion(null);
        }
    }

    private static string CodigoPredeterminado(HttpStatusCode estado)
        => estado switch
        {
            HttpStatusCode.Forbidden => "OTP_BLOQUEADO",
            HttpStatusCode.Gone => "OTP_EXPIRADO",
            HttpStatusCode.Unauthorized => "OTP_INVALIDO",
            HttpStatusCode.NotFound => "OTP_NO_ENCONTRADO",
            _ => "OTP_RECHAZADO"
        };

    private sealed record ErrorIntegracion(string? Codigo);

    private sealed record RespuestaConsumo
    {
        [JsonPropertyName("consumido")]
        public bool Consumido { get; init; }

        [JsonPropertyName("codigoError")]
        public string? CodigoError { get; init; }

        [JsonPropertyName("code")]
        public string? Code { get; init; }
    }

    private sealed record RespuestaError
    {
        [JsonPropertyName("codigo")]
        public string? Codigo { get; init; }

        [JsonPropertyName("code")]
        public string? Code { get; init; }
    }
}
