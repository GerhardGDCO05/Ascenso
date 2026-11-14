namespace Ascenso.Programas.Application.Universidades.Consultas.ConsultarUniversidades;

public sealed record class ConsultarUniversidadesQuery(
    int Pagina = 1,
    int TamanoPagina = 20,
    string? Estado = null,
    string? Busqueda = null);
