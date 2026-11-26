using Ascenso.Becas.Application.Despacho;
using Ascenso.Becas.Application.Renovacion.Dtos;

namespace Ascenso.Becas.Application.Renovacion.Comandos.SolicitarRenovacion;

public sealed record SolicitarRenovacionCommand(
    Guid BecaId, Guid PeriodoSolicitadoId, decimal MontoSolicitado)
    : ICommand<SolicitudRenovacionDto>;
