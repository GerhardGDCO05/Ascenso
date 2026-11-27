using Ascenso.Becas.Application;
using Ascenso.Becas.Application.Comun;
using Ascenso.Becas.Application.Otorgamiento.Puertos;
using Ascenso.Becas.Domain.Comun;
using Ascenso.Becas.Domain.Otorgamiento.Agregados;
using Ascenso.Becas.Domain.Ports;
using Ascenso.Becas.Infrastructure.Dependencias;
using Ascenso.Becas.Infrastructure.Trabajos;
using Ascenso.Becas.IntegrationTests.Persistencia;
using Hangfire;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Ascenso.Becas.IntegrationTests.Trabajos;

/// <summary>
/// Contenedor de dependencias de producción adaptado a las pruebas de jobs.
/// La mensajería real se sustituye por un Outbox que registra los eventos.
/// </summary>
internal static class EntornoTrabajos
{
    public static ServiceProvider Crear(
        PostgreSqlFixture fixture,
        bool conHangfire = false,
        bool conServidorHangfire = false,
        Action<OpcionesTrabajosHangfire>? configurarOpciones = null)
    {
        var servicios = new ServiceCollection();
        ConfigurarServicios(
            servicios,
            fixture.CadenaConexion,
            conHangfire,
            conServidorHangfire,
            configurarOpciones);

        return servicios.BuildServiceProvider();
    }

    public static void ConfigurarServicios(
        IServiceCollection servicios,
        string cadenaConexion,
        bool conHangfire = false,
        bool conServidorHangfire = false,
        Action<OpcionesTrabajosHangfire>? configurarOpciones = null)
    {
        ArgumentNullException.ThrowIfNull(servicios);
        ArgumentException.ThrowIfNullOrWhiteSpace(cadenaConexion);

        var opciones = new OpcionesTrabajosHangfire();
        configurarOpciones?.Invoke(opciones);

        servicios.AddLogging();
        servicios.AgregarPersistencia(cadenaConexion);
        servicios.AgregarAplicacion();
        servicios.AgregarMetricas();
        servicios.AddSingleton<IContextoOperacionActual, ContextoTrabajos>();
        servicios.AddSingleton<OutboxBecaTrabajosFalso>();
        servicios.AddSingleton<IOutboxBeca>(
            proveedor => proveedor.GetRequiredService<OutboxBecaTrabajosFalso>());
        servicios.AddSingleton(Substitute.For<IProgramasClient>());
        servicios.AddSingleton<IOptions<OpcionesTrabajosHangfire>>(
            Options.Create(opciones));
        servicios.AddScoped<ExpirarOfertaBecaJob>();
        servicios.AddScoped<ReconciliarOfertasPendientesJob>();
        servicios.AddScoped<LimpiarIdempotenciaJob>();
        servicios.AddScoped<LimpiarOutboxJob>();

        if (conHangfire)
        {
            servicios.AgregarHangfire(cadenaConexion);
            servicios.AddScoped<IProgramadorExpiracionOfertas, ProgramadorExpiracionHangfire>();
        }

        if (conServidorHangfire)
        {
            servicios.AddHangfireServer(opcionesServidor =>
            {
                opcionesServidor.Queues =
                    [RegistroHangfire.ColaCritica, RegistroHangfire.ColaPorDefecto];
                opcionesServidor.SchedulePollingInterval = TimeSpan.FromMilliseconds(500);
                opcionesServidor.WorkerCount = 1;
                opcionesServidor.ServerName = $"pruebas-{Guid.NewGuid():N}";
            });
        }
    }

    public static async Task<Beca> GuardarOfertaPendienteAsync(
        IServiceProvider proveedor,
        DateTimeOffset fechaOperacion,
        string? referenciaJob = null)
    {
        var datos = DatosPrueba.CrearDatosBeca();
        var contexto = DatosPrueba.CrearContextoOperacion(fechaOperacion);
        var beca = Beca.CrearOtorgada(datos, contexto);
        var datosOferta = DatosPrueba.CrearDatosOferta(datos);

        if (referenciaJob is not null)
        {
            datosOferta = datosOferta with
            {
                ReferenciaJobExpiracionHangfireId = referenciaJob
            };
        }

        beca.EmitirOferta(datosOferta, contexto);
        await GuardarAsync(proveedor, beca);
        return beca;
    }

    public static async Task<Beca> GuardarBecaActivaAsync(
        IServiceProvider proveedor,
        DateTimeOffset fechaOperacion)
    {
        var datos = DatosPrueba.CrearDatosBeca();
        var contexto = DatosPrueba.CrearContextoOperacion(fechaOperacion);
        var beca = DatosPrueba.CrearBecaActiva(datos, contexto);

        await GuardarAsync(proveedor, beca);
        return beca;
    }

    public static async Task<Beca?> ObtenerPorOfertaAsync(
        IServiceProvider proveedor,
        Guid ofertaId)
    {
        await using var alcance = proveedor.CreateAsyncScope();
        var repositorio = alcance.ServiceProvider.GetRequiredService<IBecaRepository>();

        return await repositorio.ObtenerPorOfertaAsync(ofertaId);
    }

    private static async Task GuardarAsync(IServiceProvider proveedor, Beca beca)
    {
        await using var alcance = proveedor.CreateAsyncScope();
        var repositorio = alcance.ServiceProvider.GetRequiredService<IBecaRepository>();
        var unidad = alcance.ServiceProvider.GetRequiredService<IUnitOfWork>();

        await unidad.EjecutarEnTransaccionAsync(
            async token =>
            {
                await repositorio.AgregarAsync(beca, token);
                await unidad.GuardarCambiosAsync(token);
            });
    }
}

internal sealed class ContextoTrabajos : IContextoOperacionActual
{
    public Guid? UsuarioId => null;

    public string CorrelationId { get; } = Guid.NewGuid().ToString("D");

    public string? DireccionIp => null;

    public Guid? AutorizacionOtpId => null;
}

internal sealed class OutboxBecaTrabajosFalso : IOutboxBeca
{
    public List<IDomainEvent> Eventos { get; } = [];

    public Task EncolarAsync(
        IDomainEvent evento,
        int aggregateVersion = 0,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(evento);
        cancellationToken.ThrowIfCancellationRequested();
        Eventos.Add(evento);
        return Task.CompletedTask;
    }
}
