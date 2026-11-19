using Ascenso.Becas.Infrastructure.Persistence.Modelos;
using Ascenso.Becas.Infrastructure.Persistence.Repositorios;
using Ascenso.Becas.Infrastructure.Tiempo;
using Ascenso.Becas.Infrastructure.Trabajos;
using Ascenso.Becas.IntegrationTests.Persistencia;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Ascenso.Becas.IntegrationTests.Trabajos;

[Collection(ColeccionPostgreSql.Nombre)]
public sealed class LimpiezaTrabajosTests
{
    private readonly PostgreSqlFixture _fixture;

    public LimpiezaTrabajosTests(PostgreSqlFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Debe_EliminarSoloLasSolicitudesIdempotentesVencidas()
    {
        await using var contexto = _fixture.CrearContexto();
        var ahora = DateTimeOffset.UtcNow;
        var vencida = CrearSolicitud(ahora.AddHours(-2), ahora.AddHours(-1));
        var vigente = CrearSolicitud(ahora, ahora.AddDays(1));

        contexto.SolicitudesIdempotencia.AddRange(vencida, vigente);
        await contexto.SaveChangesAsync();

        var opciones = Options.Create(new OpcionesTrabajosHangfire
        {
            RetencionIdempotenciaDias = 0
        });
        var job = new LimpiarIdempotenciaJob(
            new IdempotencyRepository(contexto),
            new RelojSistema(),
            opciones);

        await job.Ejecutar();

        var restantes = await contexto.SolicitudesIdempotencia
            .AsNoTracking()
            .Where(solicitud =>
                solicitud.Id == vencida.Id || solicitud.Id == vigente.Id)
            .Select(solicitud => solicitud.Id)
            .ToListAsync();

        restantes.Should().ContainSingle().Which.Should().Be(vigente.Id);
    }

    [Fact]
    public async Task Debe_EliminarSoloLosMensajesYEstadosVencidosDelOutbox()
    {
        await using var contexto = _fixture.CrearContexto();
        var ahora = DateTimeOffset.UtcNow;
        var viejo = ahora.AddDays(-8);
        var reciente = ahora.AddDays(-1);

        var estadoViejo = Guid.CreateVersion7();
        var estadoReciente = Guid.CreateVersion7();
        var estadoHuerfano = Guid.CreateVersion7();
        var mensajeViejo = Guid.CreateVersion7();
        var mensajeReciente = Guid.CreateVersion7();
        const string cuerpo = "{}";

        await contexto.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO becas.outbox_state ("OutboxId", "LockId", "Created")
            VALUES
                ({estadoViejo}, {Guid.CreateVersion7()}, {viejo}),
                ({estadoReciente}, {Guid.CreateVersion7()}, {reciente}),
                ({estadoHuerfano}, {Guid.CreateVersion7()}, {viejo});

            INSERT INTO becas.outbox_message (
                "SentTime", "MessageId", "ContentType", "MessageType", "Body", "OutboxId")
            VALUES
                ({viejo}, {mensajeViejo}, 'application/json', 'EventoViejo', {cuerpo}, {estadoViejo}),
                ({reciente}, {mensajeReciente}, 'application/json', 'EventoReciente', {cuerpo}, {estadoReciente});
            """);

        var opciones = Options.Create(new OpcionesTrabajosHangfire
        {
            RetencionOutboxDias = 7
        });
        var job = new LimpiarOutboxJob(contexto, new RelojSistema(), opciones);

        await job.Ejecutar();

        var mensajes = await contexto.Database
            .SqlQuery<Guid>($"""
                SELECT "MessageId" AS "Value"
                FROM becas.outbox_message
                WHERE "MessageId" IN ({mensajeViejo}, {mensajeReciente})
                """)
            .ToListAsync();

        var estados = await contexto.Database
            .SqlQuery<Guid>($"""
                SELECT "OutboxId" AS "Value"
                FROM becas.outbox_state
                WHERE "OutboxId" IN ({estadoViejo}, {estadoReciente}, {estadoHuerfano})
                """)
            .ToListAsync();

        mensajes.Should().ContainSingle().Which.Should().Be(mensajeReciente);
        estados.Should().ContainSingle().Which.Should().Be(estadoReciente);
    }

    private static IdempotencyRequest CrearSolicitud(
        DateTimeOffset creadoEn,
        DateTimeOffset expiraEn)
        => new()
        {
            Id = Guid.CreateVersion7(),
            Endpoint = $"/api/v1/becas/ofertas/{Guid.NewGuid():N}",
            Clave = Guid.NewGuid().ToString("N"),
            HashPayload = new string('b', 64),
            Estado = "COMPLETADA",
            StatusCode = 200,
            ResponseBody = """{"ok":true}""",
            RecursoId = Guid.CreateVersion7(),
            CreadoEn = creadoEn,
            ExpiraEn = expiraEn
        };
}
