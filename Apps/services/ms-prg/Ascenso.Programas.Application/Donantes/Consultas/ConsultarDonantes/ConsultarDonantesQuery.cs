namespace Ascenso.Programas.Application.Donantes.Consultas.ConsultarDonantes;

public sealed record class ConsultarDonantesQuery(
    int Pagina = 1,
    int TamanoPagina = 20,
    string? TipoDonante = null,
    string? EstadoDonante = null,
    string? Busqueda = null);

