namespace Ascenso.Becas.Application.Otorgamiento.Puertos;

/// <summary>
/// Métricas de negocio del otorgamiento. Las implementaciones deben ser
/// thread-safe y no bloquear el camino crítico del caso de uso.
/// </summary>
public interface IMetricasOtorgamiento
{
    void OfertaEmitida();

    void OfertaAceptada();

    void OfertaRechazada();

    void OfertaExpirada();
}
