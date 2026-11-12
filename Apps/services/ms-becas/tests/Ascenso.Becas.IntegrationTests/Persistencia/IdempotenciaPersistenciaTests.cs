using Ascenso.Becas.Infrastructure.Persistence.Modelos;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Ascenso.Becas.IntegrationTests.Persistencia;

[Collection(ColeccionPostgreSql.Nombre)]
public sealed class IdempotenciaPersistenciaTests
{
    private readonly PostgreSqlFixture _fixture;

    public IdempotenciaPersistenciaTests(PostgreSqlFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Debe_PersistirHashEstadoRespuestaYExpiracion_Cuando_SeGuardaUnaSolicitud()
    {
        await using var contexto = _fixture.CrearContexto();
        var solicitud = CrearSolicitud();
        solicitud.StatusCode = 201;
        solicitud.ResponseBody = """{"becaId":"01900000-0000-7000-8000-000000000001"}""";
        solicitud.RecursoId = Guid.CreateVersion7();

        contexto.SolicitudesIdempotencia.Add(solicitud);
        await contexto.SaveChangesAsync();
        contexto.ChangeTracker.Clear();

        var guardada = await contexto.SolicitudesIdempotencia
            .AsNoTracking()
            .SingleAsync(registro => registro.Id == solicitud.Id);

        Assert.Equal(solicitud.Endpoint, guardada.Endpoint);
        Assert.Equal(solicitud.Clave, guardada.Clave);
        Assert.Equal(solicitud.HashPayload, guardada.HashPayload);
        Assert.Equal("COMPLETADA", guardada.Estado);
        Assert.Equal(201, guardada.StatusCode);
        Assert.Equal(solicitud.ResponseBody, guardada.ResponseBody);
        Assert.Equal(solicitud.RecursoId, guardada.RecursoId);
        Assert.Equal(solicitud.ExpiraEn.ToUnixTimeMilliseconds(), guardada.ExpiraEn.ToUnixTimeMilliseconds());
    }

    [Fact]
    public async Task Debe_RechazarDuplicados_Cuando_CoincidenEndpointYClave()
    {
        await using var contexto = _fixture.CrearContexto();
        var endpoint = $"/api/v1/becas/ofertas/{Guid.NewGuid():N}";
        var clave = Guid.NewGuid().ToString("N");

        var original = CrearSolicitud(endpoint: endpoint, clave: clave);
        contexto.SolicitudesIdempotencia.Add(original);
        await contexto.SaveChangesAsync();

        var duplicada = CrearSolicitud(endpoint: endpoint, clave: clave);
        contexto.SolicitudesIdempotencia.Add(duplicada);

        var excepcion = await Assert.ThrowsAsync<DbUpdateException>(
            () => contexto.SaveChangesAsync());

        var postgres = Assert.IsType<PostgresException>(excepcion.InnerException);
        Assert.Equal(PostgresErrorCodes.UniqueViolation, postgres.SqlState);
        Assert.Equal("ux_idempotency_request_endpoint_clave", postgres.ConstraintName);
    }

    [Fact]
    public async Task Debe_PermitirLaMismaClave_Cuando_ElEndpointEsDistinto()
    {
        await using var contexto = _fixture.CrearContexto();
        var clave = Guid.NewGuid().ToString("N");

        contexto.SolicitudesIdempotencia.Add(CrearSolicitud(
            endpoint: "/api/v1/becas/ofertas",
            clave: clave));
        contexto.SolicitudesIdempotencia.Add(CrearSolicitud(
            endpoint: "/api/v1/becas/ofertas/aceptar",
            clave: clave));

        await contexto.SaveChangesAsync();

        Assert.Equal(2, await contexto.SolicitudesIdempotencia.CountAsync(
            registro => registro.Clave == clave));
    }

    private static IdempotencyRequest CrearSolicitud(
        string? endpoint = null,
        string? clave = null)
    {
        var creadoEn = DateTimeOffset.UtcNow;

        return new IdempotencyRequest
        {
            Id = Guid.CreateVersion7(),
            Endpoint = endpoint ?? $"/api/v1/becas/ofertas/{Guid.NewGuid():N}",
            Clave = clave ?? Guid.NewGuid().ToString("N"),
            HashPayload = new string('b', 64),
            Estado = "COMPLETADA",
            StatusCode = 200,
            ResponseBody = """{"ok":true}""",
            RecursoId = Guid.CreateVersion7(),
            CreadoEn = creadoEn,
            ExpiraEn = creadoEn.AddDays(7)
        };
    }
}
