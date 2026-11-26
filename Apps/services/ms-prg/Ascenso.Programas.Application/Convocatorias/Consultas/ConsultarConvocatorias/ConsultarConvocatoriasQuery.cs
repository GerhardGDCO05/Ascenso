namespace Ascenso.Programas.Application.Convocatorias.Consultas.ConsultarConvocatorias;

public sealed record ConsultarConvocatoriasQuery(
    int Pagina,
    int TamanoPagina,
    string? Estado,
    Guid? ProgramaBecaId,
    string? Busqueda);
