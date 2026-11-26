using Ascenso.Becas.Application.Despacho;

namespace Ascenso.Becas.Application.Renovacion.Comandos.ConcluirBecaPorVencimiento;

public sealed record ConcluirBecaPorVencimientoCommand(
    Guid BecaId, Guid PeriodoSolicitadoId) : ICommand<bool>;
