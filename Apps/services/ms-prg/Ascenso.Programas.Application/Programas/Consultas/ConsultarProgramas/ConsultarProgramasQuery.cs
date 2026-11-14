namespace Ascenso.Programas.Application.Programas.Consultas.ConsultarProgramas;

public sealed record ConsultarProgramasQuery(
    int Pagina = 1,
    int TamanoPagina = 20,
    string? Estado = null,
    string? PeriodoInicialId = null,
    string? ModalidadCobertura = null,
    string? Busqueda = null);
