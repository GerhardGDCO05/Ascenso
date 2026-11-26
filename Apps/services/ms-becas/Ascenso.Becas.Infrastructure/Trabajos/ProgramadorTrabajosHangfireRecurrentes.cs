using Ascenso.Becas.Infrastructure.Dependencias;
using Hangfire;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Ascenso.Becas.Infrastructure.Trabajos;

/// <summary>
/// Registra los trabajos recurrentes de reconciliación y limpieza al arrancar
/// el servicio. Hangfire los conserva en PostgreSQL y los reprograma de forma
/// idempotente en cada reinicio.
/// </summary>
public sealed class ProgramadorTrabajosHangfireRecurrentes : IHostedService
{
    public const string ReconciliacionId = "reconciliar-ofertas-pendientes";
    public const string LimpiezaIdempotenciaId = "limpiar-idempotencia";
    public const string LimpiezaOutboxId = "limpiar-outbox";
    public const string RevisionSuspensionesId = "revisar-suspensiones";

    private readonly IRecurringJobManager _gestor;
    private readonly IOptions<OpcionesTrabajosHangfire> _opciones;

    public ProgramadorTrabajosHangfireRecurrentes(
        IRecurringJobManager gestor,
        IOptions<OpcionesTrabajosHangfire> opciones)
    {
        _gestor = gestor ?? throw new ArgumentNullException(nameof(gestor));
        _opciones = opciones ?? throw new ArgumentNullException(nameof(opciones));
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        var opciones = _opciones.Value;
        var minutos = Math.Clamp(opciones.IntervaloReconciliacionMinutos, 1, 59);

        _gestor.AddOrUpdate<ReconciliarOfertasPendientesJob>(
            ReconciliacionId,
            RegistroHangfire.ColaCritica,
            job => job.Ejecutar(CancellationToken.None),
            $"*/{minutos} * * * *",
            new RecurringJobOptions
            {
                TimeZone = TimeZoneInfo.Utc
            });

        _gestor.AddOrUpdate<LimpiarIdempotenciaJob>(
            LimpiezaIdempotenciaId,
            RegistroHangfire.ColaPorDefecto,
            job => job.Ejecutar(CancellationToken.None),
            "0 3 * * *",
            new RecurringJobOptions
            {
                TimeZone = TimeZoneInfo.Utc
            });

        _gestor.AddOrUpdate<LimpiarOutboxJob>(
            LimpiezaOutboxId,
            RegistroHangfire.ColaPorDefecto,
            job => job.Ejecutar(CancellationToken.None),
            "15 3 * * *",
            new RecurringJobOptions
            {
                TimeZone = TimeZoneInfo.Utc
            });

        _gestor.AddOrUpdate<RevisarSuspensionesJob>(
            RevisionSuspensionesId,
            RegistroHangfire.ColaPorDefecto,
            job => job.Ejecutar(CancellationToken.None),
            "0 13 * * *",
            new RecurringJobOptions
            {
                TimeZone = TimeZoneInfo.Utc
            });

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
        => Task.CompletedTask;
}
