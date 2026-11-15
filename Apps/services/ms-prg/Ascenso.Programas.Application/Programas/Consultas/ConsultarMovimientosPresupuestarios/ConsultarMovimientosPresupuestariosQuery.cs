namespace Ascenso.Programas.Application.Programas.Consultas.ConsultarMovimientosPresupuestarios;

public sealed record ConsultarMovimientosPresupuestariosQuery(
    Guid ProgramaBecaId,
    int Pagina = 1,
    int TamanoPagina = 20,
    string? TipoMovimiento = null,
    string? PeriodoAcademicoId = null,
    Guid? ReferenciaExternaId = null);
