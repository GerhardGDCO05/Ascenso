using Ascenso.Becas.Application.Despacho;
using Ascenso.Becas.Application.Renovacion.Dtos;

namespace Ascenso.Becas.Application.Renovacion.Comandos.EvaluarRenovacion;

public sealed record EvaluarRenovacionCommand(
    Guid SolicitudRenovacionId, bool Aprobar, string MotivoDecision)
    : ICommand<SolicitudRenovacionDto>;
