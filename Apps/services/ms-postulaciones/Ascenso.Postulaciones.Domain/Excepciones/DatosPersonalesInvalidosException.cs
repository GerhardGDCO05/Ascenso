namespace Ascenso.Postulaciones.Domain.Excepciones;

/// <summary>
/// Se produce cuando los datos personales del postulante no son válidos.
/// </summary>
public sealed class DatosPersonalesInvalidosException : ExcepcionReglaNegocio
{
    public const string CodigoError = "DATOS_PERSONALES_INVALIDOS";

    public DatosPersonalesInvalidosException(string mensaje)
        : base(CodigoError, mensaje, 400)
    {
    }
}
