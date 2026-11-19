using Ascenso.Becas.Domain.Ports;
using Ascenso.Becas.Infrastructure.Dependencias;
using Ascenso.Becas.Infrastructure.Persistence;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Ascenso.Becas.Infrastructure.Trabajos;

/// <summary>
/// Elimina los mensajes ya enviados del Outbox y los estados huérfanos que
/// superaron la retención configurada. Los mensajes pendientes de envío se
/// conservan siempre.
/// </summary>
public sealed class LimpiarOutboxJob
{
    private readonly BecasDbContext _contexto;
    private readonly IClock _reloj;
    private readonly IOptions<OpcionesTrabajosHangfire> _opciones;

    public LimpiarOutboxJob(
        BecasDbContext contexto,
        IClock reloj,
        IOptions<OpcionesTrabajosHangfire> opciones)
    {
        _contexto = contexto ?? throw new ArgumentNullException(nameof(contexto));
        _reloj = reloj ?? throw new ArgumentNullException(nameof(reloj));
        _opciones = opciones ?? throw new ArgumentNullException(nameof(opciones));
    }

    [Queue(RegistroHangfire.ColaPorDefecto)]
    public async Task Ejecutar(CancellationToken cancellationToken = default)
    {
        var retencion = Math.Max(0, _opciones.Value.RetencionOutboxDias);
        var corte = _reloj.UtcNow.AddDays(-retencion);

        await _contexto.Database.ExecuteSqlInterpolatedAsync(
            $"""
            DELETE FROM becas.outbox_message
            WHERE "SentTime" IS NOT NULL
              AND "SentTime" < {corte}
            """,
            cancellationToken);

        await _contexto.Database.ExecuteSqlInterpolatedAsync(
            $"""
            DELETE FROM becas.outbox_state AS estado
            WHERE estado."Created" < {corte}
              AND NOT EXISTS (
                  SELECT 1
                  FROM becas.outbox_message AS mensaje
                  WHERE mensaje."OutboxId" = estado."OutboxId")
            """,
            cancellationToken);
    }
}
