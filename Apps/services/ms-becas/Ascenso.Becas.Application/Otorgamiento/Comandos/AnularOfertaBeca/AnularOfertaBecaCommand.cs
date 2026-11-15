using Ascenso.Becas.Application.Despacho;

namespace Ascenso.Becas.Application.Otorgamiento.Comandos.AnularOfertaBeca;

/// <summary>
/// Anula una oferta pendiente por decisión de coordinación.
/// </summary>
public sealed record AnularOfertaBecaCommand(Guid OfertaId, string Justificacion)
    : ICommand;
