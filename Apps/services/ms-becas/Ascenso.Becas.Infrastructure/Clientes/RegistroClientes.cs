using System.Net;
using Ascenso.Becas.Application.Correlacion;
using Ascenso.Becas.Domain.Ports;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Ascenso.Becas.Infrastructure.Clientes;

/// <summary>
/// Registro de los clientes HTTP internos. Cada cliente conserva un
/// <see cref="HttpClient"/> de larga vida con un único pool de conexiones y un
/// adaptador de ámbito de solicitud que propaga la correlación.
/// </summary>
public static class RegistroClientes
{
    private const string ClientePostulaciones = "postulaciones";
    private const string ClienteProgramas = "programas";
    private const string ClientePlataforma = "plataforma";

    public static IServiceCollection AgregarClientes(
        this IServiceCollection servicios,
        IConfiguration configuracion)
    {
        ArgumentNullException.ThrowIfNull(servicios);
        ArgumentNullException.ThrowIfNull(configuracion);

        var opciones = ClientesOptions.Leer(configuracion);
        var token = ServiceTokenOptions.Leer(configuracion);

        servicios.AddSingleton(opciones);
        servicios.AddSingleton(token);

        servicios.AddSingleton(proveedor => new ServiceTokenProvider(
            CrearClienteBase(token.TimeoutSegundos),
            token));

        servicios.AddKeyedSingleton<HttpClient>(
            ClientePostulaciones,
            (proveedor, _) => CrearClienteInterno(
                proveedor,
                opciones.Postulaciones,
                opciones.TimeoutSegundos));

        servicios.AddKeyedSingleton<HttpClient>(
            ClienteProgramas,
            (proveedor, _) => CrearClienteInterno(
                proveedor,
                opciones.Programas,
                opciones.TimeoutSegundos));

        servicios.AddKeyedSingleton<HttpClient>(
            ClientePlataforma,
            (proveedor, _) => CrearClienteInterno(
                proveedor,
                opciones.Plataforma,
                opciones.TimeoutSegundos));

        servicios.AddScoped<IPostulacionesClient>(proveedor =>
            new PostulacionesClient(
                proveedor.GetRequiredKeyedService<HttpClient>(ClientePostulaciones),
                proveedor.GetRequiredService<ICorrelacionContexto>()));

        servicios.AddScoped<IProgramasClient>(proveedor =>
            new ProgramasClient(
                proveedor.GetRequiredKeyedService<HttpClient>(ClienteProgramas),
                proveedor.GetRequiredService<ICorrelacionContexto>()));

        servicios.AddScoped<IOtpClient>(proveedor =>
            new OtpClient(
                proveedor.GetRequiredKeyedService<HttpClient>(ClientePlataforma),
                proveedor.GetRequiredService<ICorrelacionContexto>()));

        return servicios;
    }

    private static HttpClient CrearClienteInterno(
        IServiceProvider proveedor,
        ClienteHttpOptions opciones,
        int timeoutSegundos)
    {
        ArgumentNullException.ThrowIfNull(opciones);

        var baseAddress = ObtenerBase(opciones.BaseUrl);
        HttpMessageHandler handler = CrearHandler();

        if (opciones.UsaServiceToken)
        {
            handler = new TokenServicioDelegatingHandler(
                proveedor.GetRequiredService<ServiceTokenProvider>())
            {
                InnerHandler = handler
            };
        }

        return new HttpClient(handler)
        {
            BaseAddress = baseAddress,
            Timeout = TimeSpan.FromSeconds(timeoutSegundos)
        };
    }

    private static HttpClient CrearClienteBase(int timeoutSegundos)
        => new(CrearHandler())
        {
            Timeout = TimeSpan.FromSeconds(timeoutSegundos)
        };

    private static SocketsHttpHandler CrearHandler()
        => new()
        {
            PooledConnectionLifetime = TimeSpan.FromMinutes(5),
            PooledConnectionIdleTimeout = TimeSpan.FromMinutes(1),
            ConnectTimeout = TimeSpan.FromSeconds(3),
            AutomaticDecompression = DecompressionMethods.All
        };

    private static Uri ObtenerBase(string baseUrl)
    {
        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var direccion))
        {
            throw new InvalidOperationException(
                $"La URL base '{baseUrl}' de un cliente interno no es válida.");
        }

        return direccion;
    }
}
