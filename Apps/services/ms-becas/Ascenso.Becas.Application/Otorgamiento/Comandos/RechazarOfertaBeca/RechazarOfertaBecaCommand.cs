using Ascenso.Becas.Application.Despacho;

namespace Ascenso.Becas.Application.Otorgamiento.Comandos.RechazarOfertaBeca;

/// <summary>
/// Rechaza formalmente una oferta pendiente por decisión del postulante.
/// </summary>
public sealed record RechazarOfertaBecaCommand(Guid OfertaId, string? Motivo = null)
    : ICommand;
