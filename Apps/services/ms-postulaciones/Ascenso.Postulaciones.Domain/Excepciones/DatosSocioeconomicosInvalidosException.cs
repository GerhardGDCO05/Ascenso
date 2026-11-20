namespace Ascenso.Postulaciones.Domain.Excepciones;

/// <summary>
/// Se produce cuando la sección socioeconómica del postulante no es válida.
/// </summary>
public sealed class DatosSocioeconomicosInvalidosException : ExcepcionReglaNegocio
{
    public const string CodigoError = "DATOS_SOCIOECONOMICOS_INVALIDOS";

    public DatosSocioeconomicosInvalidosException(string mensaje)
        : base(CodigoError, mensaje, 400)
    {
    }
}
