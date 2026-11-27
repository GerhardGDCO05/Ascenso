namespace Ascenso.Postulaciones.Application.Consolidacion.DTOs;

public sealed record class DisponibilidadRankingDto(
    Guid ProgramaBecaId,
    int CuposAsignadosConvocatoria,
    int CuposDisponiblesPrograma,
    int CuposDisponiblesEfectivos,
    decimal PresupuestoDisponibleBruto,
    decimal PresupuestoComprometidoRenovaciones,
    decimal PresupuestoDisponibleEfectivo,
    string Moneda);
