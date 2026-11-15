namespace Ascenso.Becas.Domain.Ports;

/// <summary>
/// Respuesta del servicio de postulaciones sobre la adjudicabilidad. Incluye
/// los datos mínimos para crear la beca y la carta de otorgamiento sin exponer
/// información socioeconómica.
/// </summary>
public sealed record ResultadoAdjudicabilidad(
    Guid PostulacionId,
    bool Adjudicable,
    Guid ProgramaBecaId,
    Guid PeriodoAcademicoId,
    string PeriodoCodigo,
    Guid UniversidadAliadaId,
    Guid CarreraId,
    Guid PostulanteEstudianteId,
    string NombreEstudiante,
    string NumeroDocumentoEstudiante,
    Guid ConvocatoriaOrigenId,
    string? MotivoNoAdjudicable = null);
