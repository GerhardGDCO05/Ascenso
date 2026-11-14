namespace Ascenso.Postulaciones.Domain.Excepciones;

/// <summary>
/// Base para los recursos solicitados que no existen en el dominio de admisión.
/// </summary>
public class ExcepcionEntidadNoEncontrada : DomainException
{
    public const string CodigoError = "ENTIDAD_NO_ENCONTRADA";

    public ExcepcionEntidadNoEncontrada(string mensaje, string codigo = CodigoError)
        : base(codigo, 404, mensaje)
    {
    }
}
