namespace Ascenso.Postulaciones.Domain.Excepciones;

/// <summary>
/// Se produce cuando los datos de un miembro familiar no son válidos.
/// </summary>
public sealed class MiembroFamiliarInvalidoException : ExcepcionReglaNegocio
{
    public const string CodigoError = "MIEMBRO_FAMILIAR_INVALIDO";

    public MiembroFamiliarInvalidoException(string mensaje)
        : base(CodigoError, mensaje, 400)
    {
    }
}
