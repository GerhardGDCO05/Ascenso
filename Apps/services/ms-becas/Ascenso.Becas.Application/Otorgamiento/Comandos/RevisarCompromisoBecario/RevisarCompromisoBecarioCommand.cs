using Ascenso.Becas.Application.Despacho;
using Ascenso.Becas.Application.Otorgamiento.Dtos;

namespace Ascenso.Becas.Application.Otorgamiento.Comandos.RevisarCompromisoBecario;

/// <summary>
/// Aprueba o rechaza un compromiso firmado en revisión.
/// </summary>
public sealed record RevisarCompromisoBecarioCommand(
    Guid CompromisoId,
    bool Aprobar,
    string? Observaciones = null)
    : ICommand<CompromisoRevisadoDto>;
