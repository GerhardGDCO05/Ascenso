namespace Ascenso.Postulaciones.Domain.Enums;

/// <summary>
/// Ciclo de vida de una entrada de postulación en la lista de espera de una convocatoria.
/// </summary>
public enum EstadoListaEspera
{
    EnEspera = 1,
    ElegiblePorVacante = 2,
    Promovida = 3
}
