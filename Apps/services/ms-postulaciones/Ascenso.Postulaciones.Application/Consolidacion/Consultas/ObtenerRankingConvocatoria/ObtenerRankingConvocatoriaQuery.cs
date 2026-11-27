namespace Ascenso.Postulaciones.Application.Consolidacion.Consultas.ObtenerRankingConvocatoria;

public sealed record class ObtenerRankingConvocatoriaQuery(
    Guid ConvocatoriaId,
    string? Clasificacion = null,
    int Pagina = 1,
    int Tamano = ObtenerRankingConvocatoriaQuery.TamanoPorDefecto)
{
    public const int TamanoMinimo = 1;

    public const int TamanoPorDefecto = 20;

    public const int TamanoMaximo = 100;
}
