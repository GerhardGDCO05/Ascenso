using System.Text;
using Ascenso.Becas.Api.Errores;
using Ascenso.Becas.Application.Idempotencia;

namespace Ascenso.Becas.Api.Idempotencia;

/// <summary>
/// Exige <c>Idempotency-Key</c> en los comandos bajo <c>/api/v1</c>, registra
/// la clave y el hash dentro de la misma transacción del caso de uso y replica
/// la respuesta original en los reintentos.
/// </summary>
public sealed class IdempotenciaMiddleware
{
    private readonly RequestDelegate _siguiente;

    public IdempotenciaMiddleware(RequestDelegate siguiente)
    {
        _siguiente = siguiente ?? throw new ArgumentNullException(nameof(siguiente));
    }

    public async Task InvokeAsync(
        HttpContext contexto,
        IAlmacenIdempotencia almacen)
    {
        ArgumentNullException.ThrowIfNull(contexto);
        ArgumentNullException.ThrowIfNull(almacen);

        if (!EsComandoIdempotente(contexto.Request))
        {
            await _siguiente(contexto);
            return;
        }

        var clave = await ObtenerClaveAsync(contexto);
        if (clave is null)
        {
            return;
        }

        var endpoint = $"{contexto.Request.Method} {contexto.Request.Path.Value}";
        var hash = await HashPayload.CalcularAsync(
            contexto.Request,
            contexto.RequestAborted);
        var ahora = DateTimeOffset.UtcNow;

        var existente = await almacen.ObtenerAsync(
            endpoint,
            clave,
            contexto.RequestAborted);

        if (existente is not null && existente.ExpiraEn <= ahora)
        {
            await almacen.EliminarAsync(endpoint, clave, contexto.RequestAborted);
            existente = null;
        }

        if (existente is not null)
        {
            await ResponderExistenteAsync(contexto, existente, hash);
            return;
        }

        var registro = new RegistroIdempotencia(
            Guid.CreateVersion7(),
            endpoint,
            clave,
            hash,
            IdempotenciaConstantes.EstadoEnProceso,
            null,
            null,
            null,
            ahora,
            ahora.Add(IdempotenciaConstantes.Vigencia));

        await almacen.IniciarTransaccionAsync(contexto.RequestAborted);
        var registrado = await almacen.IntentarRegistrarAsync(
            registro,
            contexto.RequestAborted);

        if (!registrado)
        {
            await almacen.RevertirAsync(contexto.RequestAborted);

            var concurrente = await almacen.ObtenerAsync(
                endpoint,
                clave,
                contexto.RequestAborted);

            if (concurrente is null)
            {
                await ResponderEnProcesoAsync(contexto);
                return;
            }

            await ResponderExistenteAsync(contexto, concurrente, hash);
            return;
        }

        await EjecutarYGuardarRespuestaAsync(contexto, almacen, endpoint, clave);
    }

    private async Task EjecutarYGuardarRespuestaAsync(
        HttpContext contexto,
        IAlmacenIdempotencia almacen,
        string endpoint,
        string clave)
    {
        var cuerpoOriginal = contexto.Response.Body;
        using var buffer = new MemoryStream();
        contexto.Response.Body = buffer;

        try
        {
            await _siguiente(contexto);

            if (contexto.Response.StatusCode >= StatusCodes.Status500InternalServerError)
            {
                await almacen.RevertirAsync(contexto.RequestAborted);
            }
            else
            {
                buffer.Position = 0;
                var cuerpo = await new StreamReader(
                        buffer,
                        Encoding.UTF8,
                        detectEncodingFromByteOrderMarks: false,
                        leaveOpen: true)
                    .ReadToEndAsync(contexto.RequestAborted);

                await almacen.CompletarAsync(
                    endpoint,
                    clave,
                    contexto.Response.StatusCode,
                    cuerpo,
                    recursoId: null,
                    contexto.RequestAborted);

                await almacen.ConfirmarAsync(contexto.RequestAborted);
            }
        }
        catch
        {
            await almacen.RevertirAsync(contexto.RequestAborted);
            throw;
        }
        finally
        {
            contexto.Response.Body = cuerpoOriginal;
        }

        buffer.Position = 0;
        await buffer.CopyToAsync(cuerpoOriginal, contexto.RequestAborted);
    }

    private static async Task ResponderExistenteAsync(
        HttpContext contexto,
        RegistroIdempotencia registro,
        string hash)
    {
        if (!string.Equals(
                registro.HashPayload,
                hash,
                StringComparison.Ordinal))
        {
            await EscritorProblemDetails.EscribirAsync(
                contexto,
                StatusCodes.Status409Conflict,
                "IDEMPOTENCY_KEY_REUTILIZADA",
                "La clave de idempotencia ya fue usada con un payload distinto.");
            return;
        }

        if (registro.StatusCode is null
            || string.Equals(
                registro.Estado,
                IdempotenciaConstantes.EstadoEnProceso,
                StringComparison.Ordinal))
        {
            await ResponderEnProcesoAsync(contexto);
            return;
        }

        contexto.Response.StatusCode = registro.StatusCode.Value;
        contexto.Response.Headers[IdempotenciaConstantes.EncabezadoReplay] = "true";

        if (!string.IsNullOrEmpty(registro.ResponseBody))
        {
            contexto.Response.ContentType = "application/json; charset=utf-8";
            await contexto.Response.WriteAsync(
                registro.ResponseBody,
                contexto.RequestAborted);
        }
    }

    private static Task ResponderEnProcesoAsync(HttpContext contexto)
    {
        contexto.Response.Headers.RetryAfter = "1";

        return EscritorProblemDetails.EscribirAsync(
            contexto,
            StatusCodes.Status409Conflict,
            "IDEMPOTENCIA_EN_PROCESO",
            "La solicitud con esta clave aún está en proceso; reintente en un segundo.");
    }

    private static bool EsComandoIdempotente(HttpRequest request)
    {
        var esComando = HttpMethods.IsPost(request.Method)
            || HttpMethods.IsPut(request.Method)
            || HttpMethods.IsPatch(request.Method)
            || HttpMethods.IsDelete(request.Method);

        return esComando && request.Path.StartsWithSegments(
            IdempotenciaConstantes.RutaBase,
            StringComparison.OrdinalIgnoreCase);
    }

    private static async Task<string?> ObtenerClaveAsync(HttpContext contexto)
    {
        if (!contexto.Request.Headers.TryGetValue(
                IdempotenciaConstantes.Encabezado,
                out var valores))
        {
            await EscritorProblemDetails.EscribirAsync(
                contexto,
                StatusCodes.Status400BadRequest,
                "IDEMPOTENCY_KEY_REQUERIDA",
                $"El encabezado {IdempotenciaConstantes.Encabezado} es obligatorio para los comandos.");

            return null;
        }

        var clave = valores.ToString().Trim();

        if (clave.Length is < IdempotenciaConstantes.LongitudMinimaClave
            or > IdempotenciaConstantes.LongitudMaximaClave)
        {
            await EscritorProblemDetails.EscribirAsync(
                contexto,
                StatusCodes.Status400BadRequest,
                "IDEMPOTENCY_KEY_INVALIDA",
                $"La clave debe tener entre {IdempotenciaConstantes.LongitudMinimaClave} y {IdempotenciaConstantes.LongitudMaximaClave} caracteres.");

            return null;
        }

        return clave;
    }
}
