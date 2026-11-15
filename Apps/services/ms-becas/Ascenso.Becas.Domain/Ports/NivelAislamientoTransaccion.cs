namespace Ascenso.Becas.Domain.Ports;

/// <summary>
/// Nivel de aislamiento solicitado para una unidad de trabajo. El adaptador
/// de persistencia lo traduce al nivel equivalente del motor.
/// </summary>
public enum NivelAislamientoTransaccion
{
    ReadCommitted = 1,
    Serializable = 2
}
