namespace Ascenso.Becas.Application.Otorgamiento.Dtos;

/// <summary>
/// Vista de lectura de una beca con su oferta, compromiso e historial.
/// </summary>
public sealed record BecaDto(
    Guid BecaId,
    string CodigoBeca,
    Guid PostulanteEstudianteId,
    Guid PostulacionOrigenId,
    Guid ProgramaBecaId,
    string? NombrePrograma,
    Guid ConvocatoriaOrigenId,
    Guid PeriodoVigenteActualId,
    int PeriodosCursados,
    int DuracionTotalPeriodos,
    string Estado,
    string? TipoCierre,
    PoliticaCoberturaDto CoberturaAplicada,
    decimal MontoComprometidoPeriodoActual,
    decimal MontoDesembolsadoPeriodoActual,
    bool PermiteAcumulacion,
    bool RequiereCompromisoFirmado,
    bool CompromisoFirmadoAprobado,
    bool HabilitadaParaDesembolso,
    DateOnly FechaOtorgamiento,
    DateOnly? FechaActivacion,
    DateOnly? FechaConclusionORevocacion,
    OfertaBecaDto? Oferta,
    CompromisoBecarioDto? Compromiso,
    IReadOnlyList<HistorialEstadoBecaDto> Historial);
