using Ascenso.Becas.Domain.Ports;
using Ascenso.Becas.Infrastructure.Dependencias;
using Ascenso.Becas.Infrastructure.Observabilidad;
using Ascenso.Becas.Infrastructure.Persistence.Repositorios;
using Hangfire;
using Microsoft.Extensions.Options;

namespace Ascenso.Becas.Infrastructure.Trabajos;

/// <summary>
/// Elimina las solicitudes idempotentes vencidas. La retención configurable
/// permite conservarlas unos días adicionales después de su expiración.
/// </summary>
public sealed class LimpiarIdempotenciaJob
{
    private readonly IdempotencyRepository _repositorio;
    private readonly IClock _reloj;
    private readonly IOptions<OpcionesTrabajosHangfire> _opciones;

    public LimpiarIdempotenciaJob(
        IdempotencyRepository repositorio,
        IClock reloj,
        IOptions<OpcionesTrabajosHangfire> opciones)
    {
        _repositorio = repositorio ?? throw new ArgumentNullException(nameof(repositorio));
        _reloj = reloj ?? throw new ArgumentNullException(nameof(reloj));
        _opciones = opciones ?? throw new ArgumentNullException(nameof(opciones));
    }

    [Queue(RegistroHangfire.ColaPorDefecto)]
    public Task Ejecutar(CancellationToken cancellationToken = default)
        => TelemetriaHangfire.EjecutarAsync(
            nameof(LimpiarIdempotenciaJob),
            async () =>
            {
                var retencion = Math.Max(0, _opciones.Value.RetencionIdempotenciaDias);
                var corte = _reloj.UtcNow.AddDays(-retencion);

                await _repositorio.EliminarExpiradasAsync(corte, cancellationToken);
            });
}
