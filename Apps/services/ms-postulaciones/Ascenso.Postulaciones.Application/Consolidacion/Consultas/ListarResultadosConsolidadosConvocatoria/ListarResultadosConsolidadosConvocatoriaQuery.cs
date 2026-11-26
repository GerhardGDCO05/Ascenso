namespace Ascenso.Postulaciones.Application.Consolidacion.Consultas.ListarResultadosConsolidadosConvocatoria;

public sealed record class ListarResultadosConsolidadosConvocatoriaQuery(
    Guid ConvocatoriaId,
    bool? AlertaDiscrepancia = null,
    bool? DiscrepanciaRevisada = null,
    int Pagina = 1,
    int Tamano = ListarResultadosConsolidadosConvocatoriaQuery.TamanoPorDefecto)
{
    public const int TamanoMinimo = 1;

    public const int TamanoPorDefecto = 20;

    public const int TamanoMaximo = 100;
}
