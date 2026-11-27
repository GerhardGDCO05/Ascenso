namespace Ascenso.Postulaciones.Domain.Excepciones;

/// <summary>
/// Se produce cuando se solicita resolver un caso límite sin que exista un empate en frontera ni caso límite presupuestario activo.
/// </summary>
public sealed class CasoLimiteNoDetectadoException : ExcepcionReglaNegocio
{
    public const string CodigoError = "CASO_LIMITE_NO_DETECTADO";

    public CasoLimiteNoDetectadoException(string? mensaje = null)
        : base(
            CodigoError,
            string.IsNullOrWhiteSpace(mensaje)
                ? "No existe un empate en la línea de corte ni un caso límite presupuestario activo por resolver."
                : mensaje,
            422)
    {
    }
}
