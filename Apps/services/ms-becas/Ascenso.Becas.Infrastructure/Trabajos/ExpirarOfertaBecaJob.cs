using Ascenso.Becas.Application.Despacho;
using Ascenso.Becas.Application.Otorgamiento.Comandos.ExpirarOfertaBeca;
using Ascenso.Becas.Infrastructure.Dependencias;
using Hangfire;

namespace Ascenso.Becas.Infrastructure.Trabajos;

/// <summary>
/// Job de expiración de una oferta. Delega en el caso de uso idempotente, que
/// relee la oferta bajo bloqueo antes de cambiar su estado.
/// </summary>
public sealed class ExpirarOfertaBecaJob
{
    private readonly ICommandDispatcher _despachador;

    public ExpirarOfertaBecaJob(ICommandDispatcher despachador)
    {
        _despachador = despachador ?? throw new ArgumentNullException(nameof(despachador));
    }

    [Queue(RegistroHangfire.ColaCritica)]
    public Task Ejecutar(Guid ofertaId, CancellationToken cancellationToken = default)
        => _despachador.EnviarAsync<ExpirarOfertaBecaCommand, bool>(
            new ExpirarOfertaBecaCommand(ofertaId),
            cancellationToken);
}
