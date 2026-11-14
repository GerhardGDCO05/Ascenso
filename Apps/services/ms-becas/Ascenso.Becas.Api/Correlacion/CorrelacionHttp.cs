using Ascenso.Becas.Application.Correlacion;

namespace Ascenso.Becas.Api.Correlacion;

/// <summary>
/// Utilidades para leer y validar el identificador de correlación de una
/// petición HTTP.
/// </summary>
public static class CorrelacionHttp
{
    public static string Obtener(HttpContext contexto)
    {
        ArgumentNullException.ThrowIfNull(contexto);

        if (contexto.Items.TryGetValue(CorrelacionConstantes.ItemHttp, out var item)
            && item is string correlacionItem
            && !string.IsNullOrWhiteSpace(correlacionItem))
        {
            return correlacionItem;
        }

        var encabezado = contexto.Request.Headers[CorrelacionConstantes.Encabezado].ToString();
        if (EsValido(encabezado))
        {
            return encabezado.Trim();
        }

        return string.IsNullOrWhiteSpace(contexto.TraceIdentifier)
            ? Guid.NewGuid().ToString("N")
            : contexto.TraceIdentifier;
    }

    public static string? ObtenerValido(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            return null;
        }

        var recortado = valor.Trim();

        return EsValido(recortado) ? recortado : null;
    }

    public static bool EsValido(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            return false;
        }

        var recortado = valor.Trim();

        return recortado.Length <= CorrelacionConstantes.LongitudMaxima
            && recortado.All(caracter => !char.IsControl(caracter));
    }
}
