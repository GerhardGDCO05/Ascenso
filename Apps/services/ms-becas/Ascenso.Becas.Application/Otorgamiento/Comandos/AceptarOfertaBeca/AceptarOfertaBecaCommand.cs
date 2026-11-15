using Ascenso.Becas.Application.Despacho;
using Ascenso.Becas.Application.Otorgamiento.Dtos;

namespace Ascenso.Becas.Application.Otorgamiento.Comandos.AceptarOfertaBeca;

/// <summary>
/// Acepta una oferta pendiente y activa la beca.
/// </summary>
public sealed record AceptarOfertaBecaCommand(Guid OfertaId)
    : ICommand<OfertaRespondidaDto>;
