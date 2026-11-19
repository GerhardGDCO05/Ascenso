using Hangfire;

namespace Ascenso.Becas.Api.Trabajos;

/// <summary>
/// Expone el dashboard de Hangfire bajo autenticación y autorización
/// administrativa. No se publica como endpoint anónimo.
/// </summary>
public static class RegistroDashboardHangfire
{
    public const string Ruta = "/hangfire";

    public static WebApplication MapearDashboardHangfire(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.UseHangfireDashboard(Ruta, new DashboardOptions
        {
            Authorization = [new FiltroAutorizacionDashboard()],
            DashboardTitle = "Ascenso · MS-BEC"
        });

        return app;
    }
}
