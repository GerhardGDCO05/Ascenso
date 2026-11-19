using Ascenso.Becas.Application.Otorgamiento.Puertos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Ascenso.Becas.Infrastructure.Trabajos;

/// <summary>
/// Registra los jobs de Hangfire y sus opciones. El host debe haber registrado
/// previamente el almacenamiento de Hangfire.
/// </summary>
public static class RegistroTrabajosHangfire
{
    public static IServiceCollection AgregarTrabajosHangfire(
        this IServiceCollection servicios,
        IConfiguration configuracion)
    {
        ArgumentNullException.ThrowIfNull(servicios);
        ArgumentNullException.ThrowIfNull(configuracion);

        servicios
            .AddOptions<OpcionesTrabajosHangfire>()
            .Bind(configuracion.GetSection(OpcionesTrabajosHangfire.Seccion))
            .Validate(
                opciones => opciones.IntervaloReconciliacionMinutos is > 0 and <= 59,
                "El intervalo de reconciliación debe estar entre 1 y 59 minutos.")
            .Validate(
                opciones => opciones.RetencionIdempotenciaDias >= 0,
                "La retención de idempotencia no puede ser negativa.")
            .Validate(
                opciones => opciones.RetencionOutboxDias >= 0,
                "La retención del Outbox no puede ser negativa.")
            .Validate(
                opciones => opciones.LimiteOfertasPorReconciliacion > 0,
                "El límite de ofertas por reconciliación debe ser mayor que cero.")
            .ValidateOnStart();

        servicios.AddScoped<IProgramadorExpiracionOfertas, ProgramadorExpiracionHangfire>();
        servicios.AddScoped<ExpirarOfertaBecaJob>();
        servicios.AddScoped<ReconciliarOfertasPendientesJob>();
        servicios.AddScoped<LimpiarIdempotenciaJob>();
        servicios.AddScoped<LimpiarOutboxJob>();
        servicios.AddHostedService<ProgramadorTrabajosHangfireRecurrentes>();

        return servicios;
    }
}
