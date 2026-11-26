using System.Globalization;
using Ascenso.Becas.Application.IntegracionEventos;
using Ascenso.Becas.Application.Otorgamiento.Puertos;
using Ascenso.Becas.Infrastructure.Mensajeria;
using Ascenso.Becas.Infrastructure.Mensajeria.Consumidores;
using Ascenso.Becas.Infrastructure.Persistence;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Ascenso.Becas.Infrastructure.Dependencias;

/// <summary>
/// Registro de MassTransit sobre RabbitMQ con Outbox transaccional, Inbox
/// idempotente, reintentos y cola de error.
/// </summary>
public static class RegistroMensajeria
{
    public static IServiceCollection AgregarMensajeria(
        this IServiceCollection servicios,
        IConfiguration configuracion)
    {
        ArgumentNullException.ThrowIfNull(servicios);
        ArgumentNullException.ThrowIfNull(configuracion);

        var opciones = LeerOpciones(configuracion);

        if (!opciones.Habilitada)
        {
            servicios.AddScoped<IOutboxBeca, OutboxBecaNoDisponible>();
            return servicios;
        }

        servicios.AddScoped<IOutboxBeca, OutboxBeca>();

        servicios.AddMassTransit(configuracionBus =>
        {
            configuracionBus.AddEntityFrameworkOutbox<BecasDbContext>(outbox =>
            {
                outbox.UsePostgres();
                outbox.UseBusOutbox();
            });

            configuracionBus.AddConsumer<ProgramaBecaConfiguradoConsumidor>();
            configuracionBus.AddConsumer<CostoMatriculaActualizadoConsumidor>();
            configuracionBus.AddConsumer<ConvenioUniversidadRegistradoConsumidor>();
            configuracionBus.AddConsumer<ConvenioUniversidadVencidoConsumidor>();

            configuracionBus.UsingRabbitMq((contexto, transporte) =>
            {
                transporte.Host(
                    opciones.Host,
                    opciones.Port,
                    opciones.VirtualHost,
                    host =>
                    {
                        host.Username(opciones.Username);
                        host.Password(opciones.Password);
                    });

                ConfigurarIntercambiosSalida(transporte);
                ConfigurarIntercambiosEntrada(transporte);

                ConfigurarCola<ProgramaBecaConfiguradoConsumidor>(
                    transporte,
                    contexto,
                    NombresMensajeria.ColaProgramaBecaConfigurado,
                    opciones.PrefetchCount);

                ConfigurarCola<CostoMatriculaActualizadoConsumidor>(
                    transporte,
                    contexto,
                    NombresMensajeria.ColaCostoMatriculaActualizado,
                    opciones.PrefetchCount);

                ConfigurarCola<ConvenioUniversidadRegistradoConsumidor>(
                    transporte,
                    contexto,
                    NombresMensajeria.ColaConvenioUniversidadRegistrado,
                    opciones.PrefetchCount);

                ConfigurarCola<ConvenioUniversidadVencidoConsumidor>(
                    transporte,
                    contexto,
                    NombresMensajeria.ColaConvenioUniversidadVencido,
                    opciones.PrefetchCount);
            });
        });

        return servicios;
    }

    private static void ConfigurarIntercambiosSalida(IRabbitMqBusFactoryConfigurator transporte)
    {
        transporte.Message<EventoIntegracion<OfertaBecaEmitida>>(
            mensaje => mensaje.SetEntityName(
                NombresMensajeria.IntercambioSalida(nameof(OfertaBecaEmitida))));

        transporte.Message<EventoIntegracion<BecaActivada>>(
            mensaje => mensaje.SetEntityName(
                NombresMensajeria.IntercambioSalida(nameof(BecaActivada))));

        transporte.Message<EventoIntegracion<OfertaBecaRechazada>>(
            mensaje => mensaje.SetEntityName(
                NombresMensajeria.IntercambioSalida(nameof(OfertaBecaRechazada))));

        transporte.Message<EventoIntegracion<OfertaBecaAnulada>>(
            mensaje => mensaje.SetEntityName(
                NombresMensajeria.IntercambioSalida(nameof(OfertaBecaAnulada))));

        transporte.Message<EventoIntegracion<OfertaBecaExpirada>>(
            mensaje => mensaje.SetEntityName(
                NombresMensajeria.IntercambioSalida(nameof(OfertaBecaExpirada))));

        transporte.Message<EventoIntegracion<CompromisoBecarioCargado>>(
            mensaje => mensaje.SetEntityName(
                NombresMensajeria.IntercambioSalida(nameof(CompromisoBecarioCargado))));

        transporte.Message<EventoIntegracion<CompromisoBecarioAprobado>>(
            mensaje => mensaje.SetEntityName(
                NombresMensajeria.IntercambioSalida(nameof(CompromisoBecarioAprobado))));

        transporte.Message<EventoIntegracion<CompromisoBecarioRechazado>>(
            mensaje => mensaje.SetEntityName(
                NombresMensajeria.IntercambioSalida(nameof(CompromisoBecarioRechazado))));

        transporte.Message<EventoIntegracion<DesembolsoProgramado>>(
            mensaje => mensaje.SetEntityName(NombresMensajeria.IntercambioSalida(nameof(DesembolsoProgramado))));
        transporte.Message<EventoIntegracion<DesembolsoConfirmadoConOTP>>(
            mensaje => mensaje.SetEntityName(NombresMensajeria.IntercambioSalida(nameof(DesembolsoConfirmadoConOTP))));
        transporte.Message<EventoIntegracion<DesembolsoFallido>>(
            mensaje => mensaje.SetEntityName(NombresMensajeria.IntercambioSalida(nameof(DesembolsoFallido))));
        transporte.Message<EventoIntegracion<DesembolsoDevuelto>>(
            mensaje => mensaje.SetEntityName(NombresMensajeria.IntercambioSalida(nameof(DesembolsoDevuelto))));
        transporte.Message<EventoIntegracion<DesembolsoReintentado>>(
            mensaje => mensaje.SetEntityName(NombresMensajeria.IntercambioSalida(nameof(DesembolsoReintentado))));
        transporte.Message<EventoIntegracion<DesembolsoCancelado>>(
            mensaje => mensaje.SetEntityName(NombresMensajeria.IntercambioSalida(nameof(DesembolsoCancelado))));
        transporte.Message<EventoIntegracion<DesembolsosBecaSuspendidos>>(
            mensaje => mensaje.SetEntityName(NombresMensajeria.IntercambioSalida(nameof(DesembolsosBecaSuspendidos))));
        transporte.Message<EventoIntegracion<DesembolsosBecaReanudados>>(
            mensaje => mensaje.SetEntityName(NombresMensajeria.IntercambioSalida(nameof(DesembolsosBecaReanudados))));
        transporte.Message<EventoIntegracion<BecaRevocadaDefinitivamente>>(
            mensaje => mensaje.SetEntityName(NombresMensajeria.IntercambioSalida(nameof(BecaRevocadaDefinitivamente))));
    }

    private static void ConfigurarIntercambiosEntrada(IRabbitMqBusFactoryConfigurator transporte)
    {
        transporte.Message<EventoIntegracion<ProgramaBecaConfigurado>>(
            mensaje => mensaje.SetEntityName(
                NombresMensajeria.IntercambioEntrada(nameof(ProgramaBecaConfigurado))));

        transporte.Message<EventoIntegracion<CostoMatriculaActualizado>>(
            mensaje => mensaje.SetEntityName(
                NombresMensajeria.IntercambioEntrada(nameof(CostoMatriculaActualizado))));

        transporte.Message<EventoIntegracion<ConvenioUniversidadRegistrado>>(
            mensaje => mensaje.SetEntityName(
                NombresMensajeria.IntercambioEntrada(nameof(ConvenioUniversidadRegistrado))));

        transporte.Message<EventoIntegracion<ConvenioUniversidadVencido>>(
            mensaje => mensaje.SetEntityName(
                NombresMensajeria.IntercambioEntrada(nameof(ConvenioUniversidadVencido))));
    }

    private static void ConfigurarCola<TConsumidor>(
        IRabbitMqBusFactoryConfigurator transporte,
        IBusRegistrationContext contexto,
        string cola,
        ushort prefetchCount)
        where TConsumidor : class, IConsumer
    {
        transporte.ReceiveEndpoint(cola, endpoint =>
        {
            endpoint.PrefetchCount = prefetchCount;
            endpoint.UseMessageRetry(reintentos => reintentos.Exponential(
                5,
                TimeSpan.FromSeconds(1),
                TimeSpan.FromSeconds(30),
                TimeSpan.FromSeconds(5)));
            endpoint.UseEntityFrameworkOutbox<BecasDbContext>(contexto);
            endpoint.ConfigureConsumer<TConsumidor>(contexto);
        });
    }

    private static RabbitMqOptions LeerOpciones(IConfiguration configuracion)
        => new()
        {
            Habilitada = !string.Equals(
                configuracion["Mensajeria:Habilitada"],
                "false",
                StringComparison.OrdinalIgnoreCase),
            Host = configuracion["RabbitMq:Host"] ?? "localhost",
            Port = LeerPuerto(configuracion["RabbitMq:Port"], 5672),
            VirtualHost = configuracion["RabbitMq:VirtualHost"] ?? "/",
            Username = configuracion["RabbitMq:Username"] ?? "guest",
            Password = configuracion["RabbitMq:Password"] ?? "guest",
            PrefetchCount = LeerPuerto(configuracion["RabbitMq:PrefetchCount"], 16)
        };

    private static ushort LeerPuerto(string? valor, ushort predeterminado)
        => ushort.TryParse(
            valor,
            NumberStyles.Integer,
            CultureInfo.InvariantCulture,
            out var puerto)
            ? puerto
            : predeterminado;
}
