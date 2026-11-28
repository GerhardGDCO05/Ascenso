namespace Ascenso.Postulaciones.Application.ListaEspera.Consultas.ObtenerListaEsperaConvocatoria;

public sealed record class ObtenerListaEsperaConvocatoriaQuery(
    Guid ConvocatoriaId,
    string? Estado = null,
    int Pagina = 1,
    int Tamano = ObtenerListaEsperaConvocatoriaQuery.TamanoPorDefecto)
{
    public const int TamanoMinimo = 1;

    public const int TamanoPorDefecto = 20;

    public const int TamanoMaximo = 100;
}
