using Ascenso.Programas.Application.Convocatorias.Comandos.CerrarConvocatoriasVencidas;
using Hangfire;

namespace Ascenso.Programas.Infrastructure.Trabajos;

/// <summary>
/// Trabajo recurrente de Hangfire que cierra las convocatorias publicadas vencidas.
/// </summary>
public sealed class CerrarConvocatoriasVencidasJob
{
    private readonly CerrarConvocatoriasVencidasCommandHandler _handler;

    public CerrarConvocatoriasVencidasJob(CerrarConvocatoriasVencidasCommandHandler handler)
    {
        _handler = handler ?? throw new ArgumentNullException(nameof(handler));
    }

    [DisableConcurrentExecution(timeoutInSeconds: 300)]
    public Task<int> EjecutarAsync(CancellationToken cancellationToken = default)
        => _handler.HandleAsync(
            new CerrarConvocatoriasVencidasCommand(CerrarConvocatoriasVencidasCommand.UsuarioSistemaPorDefecto),
            cancellationToken);
}
