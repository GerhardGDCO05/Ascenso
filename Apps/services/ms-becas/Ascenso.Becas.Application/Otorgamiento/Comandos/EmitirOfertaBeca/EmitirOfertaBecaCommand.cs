using Ascenso.Becas.Application.Despacho;
using Ascenso.Becas.Application.Otorgamiento.Dtos;

namespace Ascenso.Becas.Application.Otorgamiento.Comandos.EmitirOfertaBeca;

/// <summary>
/// Emite una oferta de beca para una postulación adjudicable.
/// </summary>
public sealed record EmitirOfertaBecaCommand(Guid PostulacionId)
    : ICommand<OfertaEmitidaDto>;
