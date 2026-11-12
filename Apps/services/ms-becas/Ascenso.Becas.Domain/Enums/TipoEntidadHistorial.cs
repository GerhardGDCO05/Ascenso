namespace Ascenso.Becas.Domain.Enums;

/// <summary>
/// Entidad cuyo cambio de estado queda registrado en el historial append-only.
/// </summary>
public enum TipoEntidadHistorial
{
    Beca = 1,
    OfertaBeca = 2,
    CompromisoBecario = 3
}
