using Ascenso.Becas.Application.Despacho;
using Ascenso.Becas.Application.Otorgamiento.Comandos.ExpirarOfertaBeca;
using Ascenso.Becas.Domain.Ports;
using Ascenso.Becas.Infrastructure.Dependencias;
using Ascenso.Becas.Infrastructure.Observabilidad;
using Hangfire;
using Microsoft.Extensions.Options;

namespace Ascenso.Becas.Infrastructure.Trabajos;

/// <summary>
/// Red de seguridad para las ofertas vencidas que no tienen job programado o
/// cuyo job se perdió. Procesa la misma lógica idempotente de expiración.
/// </summary>
public sealed class ReconciliarOfertasPendientesJob
{
    private readonly IOfertaBecaRepository _ofertas;
    private readonly ICommandDispatcher _despachador;
    private readonly IClock _reloj;
    private readonly IOptions<OpcionesTrabajosHangfire> _opciones;

    public ReconciliarOfertasPendientesJob(
        IOfertaBecaRepository ofertas,
        ICommandDispatcher despachador,
        IClock reloj,
        IOptions<OpcionesTrabajosHangfire> opciones)
    {
        _ofertas = ofertas ?? throw new ArgumentNullException(nameof(ofertas));
        _despachador = despachador ?? throw new ArgumentNullException(nameof(despachador));
        _reloj = reloj ?? throw new ArgumentNullException(nameof(reloj));
        _opciones = opciones ?? throw new ArgumentNullException(nameof(opciones));
    }

    [Queue(RegistroHangfire.ColaCritica)]
    public Task Ejecutar(CancellationToken cancellationToken = default)
        => TelemetriaHangfire.EjecutarAsync(
            nameof(ReconciliarOfertasPendientesJob),
            () => EjecutarAsync(cancellationToken));

    private async Task EjecutarAsync(CancellationToken cancellationToken)
    {
        var limite = Math.Max(1, _opciones.Value.LimiteOfertasPorReconciliacion);
        var vencidas = await _ofertas.ObtenerPendientesVencidasAsync(
            _reloj.UtcNow,
            limite,
            cancellationToken);

        foreach (var ofertaId in vencidas)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await _despachador.EnviarAsync<ExpirarOfertaBecaCommand, bool>(
                new ExpirarOfertaBecaCommand(ofertaId),
                cancellationToken);
        }
    }
}
