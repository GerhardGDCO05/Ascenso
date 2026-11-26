namespace Ascenso.Becas.Application.Excepciones;

/// <summary>
/// Se produce cuando MS-POS rechaza la transición de la postulación a
/// OTORGADA.
/// </summary>
public sealed class PostulacionOtorgamientoRechazadaException : AplicacionException
{
    public PostulacionOtorgamientoRechazadaException(
        string codigo,
        string mensaje,
        int estadoHttp)
        : base(codigo, estadoHttp, mensaje)
    {
        if (estadoHttp is not (409 or 422))
        {
            throw new ArgumentOutOfRangeException(
                nameof(estadoHttp),
                "MS-POS solo puede rechazar el otorgamiento con 409 o 422.");
        }
    }
}
