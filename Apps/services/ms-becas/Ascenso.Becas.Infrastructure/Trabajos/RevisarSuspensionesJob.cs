using Ascenso.Becas.Domain.Enums;
using Ascenso.Becas.Domain.Otorgamiento.Entidades;
using Ascenso.Becas.Domain.Ports;
using Ascenso.Becas.Infrastructure.Dependencias;
using Ascenso.Becas.Infrastructure.Observabilidad;
using Ascenso.Becas.Infrastructure.Persistence;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Ascenso.Becas.Infrastructure.Trabajos;

/// <summary>
/// Emite alertas operativas para suspensiones que requieren revisión humana.
/// Solo consulta el estado; nunca reactiva una beca ni sus cuotas.
/// </summary>
public sealed class RevisarSuspensionesJob
{
    private static readonly Action<ILogger, Guid, Guid, DateOnly?, bool, Exception?>
        _alertaRevision = LoggerMessage.Define<Guid, Guid, DateOnly?, bool>(
            LogLevel.Warning, new EventId(1708, "SuspensionPendienteRevision"),
            "Suspensión de beca pendiente de revisión: SuspensionId={SuspensionId}, BecaId={BecaId}, FechaRevision={FechaRevision}, EsIndefinida={EsIndefinida}");

    private readonly BecasDbContext _contexto;
    private readonly IClock _reloj;
    private readonly IOptions<OpcionesTrabajosHangfire> _opciones;
    private readonly ILogger<RevisarSuspensionesJob> _logger;

    public RevisarSuspensionesJob(BecasDbContext contexto, IClock reloj,
        IOptions<OpcionesTrabajosHangfire> opciones,
        ILogger<RevisarSuspensionesJob> logger)
    {
        _contexto = contexto ?? throw new ArgumentNullException(nameof(contexto));
        _reloj = reloj ?? throw new ArgumentNullException(nameof(reloj));
        _opciones = opciones ?? throw new ArgumentNullException(nameof(opciones));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [Queue(RegistroHangfire.ColaPorDefecto)]
    public Task Ejecutar(CancellationToken cancellationToken = default)
        => TelemetriaHangfire.EjecutarAsync(nameof(RevisarSuspensionesJob),
            () => EjecutarAsync(cancellationToken));

    private async Task EjecutarAsync(CancellationToken cancellationToken)
    {
        var hoy = DateOnly.FromDateTime(_reloj.UtcNow.UtcDateTime);
        var tamanoPagina = Math.Max(1, _opciones.Value.LimiteSuspensionesPorPagina);
        var pagina = 0;
        while (true)
        {
            var suspensiones = await _contexto.Set<SuspensionDesembolso>()
                .AsNoTracking()
                .Where(item => item.Estado == EstadoSuspension.Vigente
                    && item.FechaInicio <= hoy
                    && (item.EsIndefinida || item.FechaRevisionProgramada <= hoy))
                .OrderBy(item => item.Id)
                .Skip(pagina * tamanoPagina)
                .Take(tamanoPagina)
                .Select(item => new
                {
                    item.Id,
                    item.BecaId,
                    item.FechaRevisionProgramada,
                    item.EsIndefinida
                })
                .ToArrayAsync(cancellationToken);

            foreach (var suspension in suspensiones)
            {
                _alertaRevision(_logger,
                    suspension.Id, suspension.BecaId,
                    suspension.FechaRevisionProgramada, suspension.EsIndefinida, null);
            }

            if (suspensiones.Length < tamanoPagina)
                break;
            pagina++;
        }
    }
}
