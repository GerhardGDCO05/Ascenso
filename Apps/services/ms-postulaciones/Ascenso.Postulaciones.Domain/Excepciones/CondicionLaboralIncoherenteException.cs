namespace Ascenso.Postulaciones.Domain.Excepciones;

/// <summary>
/// Se produce cuando la ocupación no es coherente con la condición laboral.
/// </summary>
public sealed class CondicionLaboralIncoherenteException : ExcepcionReglaNegocio
{
    public const string CodigoError = "CONDICION_LABORAL_INCOHERENTE";

    public CondicionLaboralIncoherenteException(string mensaje)
        : base(CodigoError, mensaje)
    {
    }
}
