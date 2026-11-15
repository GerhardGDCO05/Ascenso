namespace Ascenso.Becas.Application.Excepciones;

/// <summary>
/// Se produce cuando el servicio de programas rechaza la reserva de cupo y
/// presupuesto. Conserva el código devuelto por el servicio para que el
/// cliente reciba un error estable.
/// </summary>
public sealed class ReservaPresupuestoRechazadaException : AplicacionException
{
    public const string CodigoSinCupoOPresupuesto = "SIN_CUPO_O_PRESUPUESTO";
    public const string CodigoConvenioVencido = "CONVENIO_VENCIDO";

    public ReservaPresupuestoRechazadaException(
        string codigo,
        string mensaje,
        int estadoHttp = 409)
        : base(codigo, estadoHttp, mensaje)
    {
    }

    public static ReservaPresupuestoRechazadaException PorFaltaDeCupoOPresupuesto()
        => new(
            CodigoSinCupoOPresupuesto,
            "El programa no tiene cupo o presupuesto disponible para la oferta.",
            409);

    public static ReservaPresupuestoRechazadaException PorConvenioVencido()
        => new(
            CodigoConvenioVencido,
            "El convenio con la universidad no está vigente para emitir la oferta.",
            422);
}
