using System.Globalization;

namespace Ascenso.Postulaciones.Domain.Excepciones;

/// <summary>
/// Se produce cuando el puntaje asignado a un criterio se encuentra fuera de su rango permitido.
/// </summary>
public sealed class PuntajeFueraDeRangoException : ExcepcionReglaNegocio
{
    public const string CodigoError = "PUNTAJE_FUERA_DE_RANGO";

    public PuntajeFueraDeRangoException(string? mensaje = null)
        : base(
            CodigoError,
            string.IsNullOrWhiteSpace(mensaje)
                ? "El puntaje asignado al criterio se encuentra fuera del rango permitido."
                : mensaje,
            422)
    {
    }

    public PuntajeFueraDeRangoException(
        string nombreCriterio,
        decimal puntajeMinimo,
        decimal puntajeMaximo,
        decimal puntajeAsignado)
        : base(
            CodigoError,
            string.Create(
                CultureInfo.InvariantCulture,
                $"El puntaje {puntajeAsignado:0.00} para el criterio '{nombreCriterio}' está fuera del rango permitido [{puntajeMinimo:0.00}, {puntajeMaximo:0.00}]."),
            422)
    {
    }
}
