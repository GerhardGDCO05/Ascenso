using Ascenso.Becas.Application.Despacho;
using Ascenso.Becas.Application.Renovacion.Comandos.ConcluirBecaPorVencimiento;
using Ascenso.Becas.Infrastructure.Dependencias;
using Ascenso.Becas.Infrastructure.Observabilidad;
using Hangfire;

namespace Ascenso.Becas.Infrastructure.Trabajos;

/// <summary>Relee la beca y la ventana al ejecutarse; no concluye antes de plazo.</summary>
public sealed class ConcluirBecaJob
{
    private readonly ICommandDispatcher _despachador;

    public ConcluirBecaJob(ICommandDispatcher despachador)
        => _despachador = despachador ?? throw new ArgumentNullException(nameof(despachador));

    [Queue(RegistroHangfire.ColaCritica)]
    public Task Ejecutar(Guid becaId, Guid periodoSolicitadoId,
        CancellationToken cancellationToken = default)
        => TelemetriaHangfire.EjecutarAsync<bool>(nameof(ConcluirBecaJob),
            () => _despachador.EnviarAsync<ConcluirBecaPorVencimientoCommand, bool>(
                new ConcluirBecaPorVencimientoCommand(becaId, periodoSolicitadoId),
                cancellationToken));
}
