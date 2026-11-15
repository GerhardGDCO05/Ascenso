using Ascenso.Becas.Application.Excepciones;
using Ascenso.Becas.Domain.Excepciones;
using Microsoft.EntityFrameworkCore;

namespace Ascenso.Becas.Api.Errores;

/// <summary>
/// Traduce excepciones de dominio, aplicación e infraestructura a estatus HTTP
/// y códigos de error estables.
/// </summary>
public static class MapeadorExcepcionesDominio
{
    public static ResultadoError Mapear(Exception excepcion)
    {
        ArgumentNullException.ThrowIfNull(excepcion);

        return excepcion switch
        {
            ValidacionAplicacionException validacion => new ResultadoError(
                validacion.EstadoHttp,
                validacion.Codigo,
                validacion.Message,
                validacion.Errores),
            AplicacionException aplicacion => new ResultadoError(
                aplicacion.EstadoHttp,
                aplicacion.Codigo,
                aplicacion.Message),
            DomainException dominio => new ResultadoError(
                dominio.EstadoHttp,
                dominio.Codigo,
                dominio.Message),
            DbUpdateConcurrencyException => new ResultadoError(
                409,
                "CONCURRENCY_CONFLICT",
                "La operación compite con otra modificación de los mismos datos. Reintente la solicitud."),
            DbUpdateException => new ResultadoError(
                409,
                "CONFLICTO_PERSISTENCIA",
                "No fue posible guardar los cambios por un conflicto de persistencia."),
            BadHttpRequestException solicitud => new ResultadoError(
                solicitud.StatusCode,
                "SOLICITUD_INVALIDA",
                "La solicitud no es válida."),
            OperationCanceledException => new ResultadoError(
                499,
                "OPERACION_CANCELADA",
                "La operación fue cancelada por el cliente."),
            _ => new ResultadoError(
                500,
                "ERROR_INTERNO",
                "Ocurrió un error interno. Contacte al soporte con el identificador de correlación.")
        };
    }
}

/// <summary>
/// Resultado del mapeo de una excepción a una respuesta HTTP.
/// </summary>
public sealed record ResultadoError(
    int EstadoHttp,
    string Codigo,
    string Mensaje,
    IReadOnlyDictionary<string, string[]>? Errores = null);
