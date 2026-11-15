namespace Ascenso.Postulaciones.Domain.Excepciones;

/// <summary>
/// Base para las violaciones de reglas de negocio del dominio de admisión.
/// </summary>
public class ExcepcionReglaNegocio : DomainException
{
    public ExcepcionReglaNegocio(string codigo, string mensaje, int estadoHttp = 422)
        : base(codigo, estadoHttp, mensaje)
    {
    }
}
