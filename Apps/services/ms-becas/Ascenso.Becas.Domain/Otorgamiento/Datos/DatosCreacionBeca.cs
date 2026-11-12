using Ascenso.Becas.Domain.ValueObjects;

namespace Ascenso.Becas.Domain.Otorgamiento.Datos;

/// <summary>
/// Datos de negocio necesarios para otorgar una beca por primera vez.
/// </summary>
public sealed record class DatosCreacionBeca(
    Guid PostulanteEstudianteId,
    string NumeroDocumentoEstudiante,
    Guid PostulacionOrigenId,
    Guid ProgramaBecaId,
    Guid ConvocatoriaOrigenId,
    Guid PeriodoOtorgamientoId,
    int DuracionTotalPeriodos,
    PoliticaCobertura CoberturaAplicada,
    Guid CostoMatriculaReferenciaSnapshotId,
    decimal MontoComprometidoPeriodoActual,
    bool PermiteAcumulacion,
    bool RequiereCompromisoFirmado,
    string CodigoBeca);
