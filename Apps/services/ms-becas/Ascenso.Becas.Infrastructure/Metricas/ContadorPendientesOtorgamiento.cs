using Ascenso.Becas.Domain.Enums;
using Ascenso.Becas.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Ascenso.Becas.Infrastructure.Metricas;

/// <summary>
/// Consulta el número de compromisos pendientes y de mensajes del Outbox sin
/// enviar. Usa un alcance propio para no compartir el <c>DbContext</c> de una
/// petición.
/// </summary>
public sealed class ContadorPendientesOtorgamiento
    : IContadorPendientesOtorgamiento
{
    private readonly IServiceScopeFactory _alcances;

    public ContadorPendientesOtorgamiento(IServiceScopeFactory alcances)
    {
        _alcances = alcances ?? throw new ArgumentNullException(nameof(alcances));
    }

    public async Task<ResultadoPendientesOtorgamiento> ContarAsync(
        CancellationToken cancellationToken = default)
    {
        await using var alcance = _alcances.CreateAsyncScope();
        var contexto = alcance.ServiceProvider.GetRequiredService<BecasDbContext>();

        var compromisos = await contexto.CompromisosBecarios
            .AsNoTracking()
            .LongCountAsync(
                compromiso => compromiso.Estado == EstadoCompromiso.PendienteCarga
                    || compromiso.Estado == EstadoCompromiso.EnRevision,
                cancellationToken);

        var outbox = await contexto.Database
            .SqlQueryRaw<long>(
                """
                SELECT COUNT(*)::bigint AS "Value"
                FROM becas.outbox_message
                WHERE "SentTime" IS NULL
                """)
            .SingleAsync(cancellationToken);

        return new ResultadoPendientesOtorgamiento(compromisos, outbox);
    }
}
