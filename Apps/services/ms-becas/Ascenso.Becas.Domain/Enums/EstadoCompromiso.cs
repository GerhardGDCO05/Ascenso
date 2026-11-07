namespace Ascenso.Becas.Domain.Enums;

/// <summary>
/// Ciclo de vida del compromiso firmado del becario.
/// </summary>
public enum EstadoCompromiso
{
    PendienteCarga = 1,
    EnRevision = 2,
    Aprobado = 3,
    Rechazado = 4
}
