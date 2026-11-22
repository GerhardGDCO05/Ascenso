using System.Globalization;
using OpenTelemetry.Exporter;

namespace Ascenso.Becas.Api.Observabilidad;

/// <summary>
/// Opciones de observabilidad leídas de la sección <c>Observabilidad</c>.
/// Mantiene la telemetría deshabilitada si no se configura un exportador,
/// pero permite que las métricas de negocio se sigan publicando.
/// </summary>
public sealed class OpcionesTelemetria
{
    public const string Seccion = "Observabilidad";

    public bool Habilitada { get; init; } = true;

    public string NombreServicio { get; init; } = "ms-bec";

    public Uri? EndpointOtlp { get; init; }

    public OtlpExportProtocol Protocolo { get; init; } = OtlpExportProtocol.Grpc;

    public TimeSpan IntervaloExportacion { get; init; } = TimeSpan.FromSeconds(10);

    public bool MetricasPendientesHabilitadas { get; init; } = true;

    public TimeSpan IntervaloMetricasPendientes { get; init; } =
        TimeSpan.FromSeconds(30);

    public static OpcionesTelemetria Leer(IConfiguration configuracion)
    {
        ArgumentNullException.ThrowIfNull(configuracion);

        var telemetria = configuracion.GetSection($"{Seccion}:Telemetria");
        var pendientes = configuracion.GetSection($"{Seccion}:MetricasPendientes");

        return new OpcionesTelemetria
        {
            Habilitada = LeerBooleano(telemetria["Habilitada"], true),
            NombreServicio = telemetria["NombreServicio"] is { Length: > 0 } nombre
                ? nombre
                : "ms-bec",
            EndpointOtlp = LeerEndpoint(telemetria["EndpointOtlp"]),
            Protocolo = LeerProtocolo(telemetria["Protocolo"]),
            IntervaloExportacion = LeerIntervalo(
                telemetria["IntervaloExportacionSegundos"],
                TimeSpan.FromSeconds(10)),
            MetricasPendientesHabilitadas = LeerBooleano(
                pendientes["Habilitada"],
                true),
            IntervaloMetricasPendientes = LeerIntervalo(
                pendientes["IntervaloSegundos"],
                TimeSpan.FromSeconds(30))
        };
    }

    private static bool LeerBooleano(string? valor, bool predeterminado)
        => bool.TryParse(valor, out var resultado) ? resultado : predeterminado;

    private static Uri? LeerEndpoint(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            return null;
        }

        if (!Uri.TryCreate(valor, UriKind.Absolute, out var endpoint))
        {
            throw new InvalidOperationException(
                $"El endpoint OTLP '{valor}' no es una URI absoluta válida.");
        }

        return endpoint;
    }

    private static OtlpExportProtocol LeerProtocolo(string? valor)
        => Enum.TryParse<OtlpExportProtocol>(
            valor,
            ignoreCase: true,
            out var protocolo)
            ? protocolo
            : OtlpExportProtocol.Grpc;

    private static TimeSpan LeerIntervalo(string? valor, TimeSpan predeterminado)
        => double.TryParse(
                valor,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out var segundos)
            && segundos > 0
            ? TimeSpan.FromSeconds(segundos)
            : predeterminado;
}
