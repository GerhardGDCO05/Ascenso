using Ascenso.Becas.Application.Idempotencia;
using Ascenso.Becas.Infrastructure.Persistence.Repositorios;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Ascenso.Becas.IntegrationTests.Persistencia;

[Collection(ColeccionPostgreSql.Nombre)]
public sealed class AlmacenIdempotenciaTests
{
    private readonly PostgreSqlFixture _fixture;

    public AlmacenIdempotenciaTests(PostgreSqlFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Debe_RegistrarCompletarYLeer_DentroDeLaTransaccion()
    {
        await using var contexto = _fixture.CrearContexto();
        var almacen = new AlmacenIdempotencia(contexto);
        var registro = CrearRegistro();
        var recursoId = Guid.CreateVersion7();

        await almacen.IniciarTransaccionAsync();
        (await almacen.IntentarRegistrarAsync(registro)).Should().BeTrue();
        await almacen.CompletarAsync(
            registro.Endpoint,
            registro.Clave,
            StatusCodes.Status201Created,
            """{"id":"1"}""",
            recursoId);
        await almacen.ConfirmarAsync();

        await using var lectura = _fixture.CrearContexto();
        var recuperado = await new AlmacenIdempotencia(lectura)
            .ObtenerAsync(registro.Endpoint, registro.Clave);

        recuperado.Should().NotBeNull();
        recuperado!.Estado.Should().Be("COMPLETADA");
        recuperado.StatusCode.Should().Be(StatusCodes.Status201Created);
        recuperado.ResponseBody.Should().Be("""{"id":"1"}""");
        recuperado.RecursoId.Should().Be(recursoId);
    }

    [Fact]
    public async Task Debe_NoDuplicar_Cuando_LaClaveYaExisteParaElEndpoint()
    {
        await using var contexto = _fixture.CrearContexto();
        var almacen = new AlmacenIdempotencia(contexto);
        var original = CrearRegistro();

        await almacen.IniciarTransaccionAsync();
        (await almacen.IntentarRegistrarAsync(original)).Should().BeTrue();
        await almacen.CompletarAsync(
            original.Endpoint,
            original.Clave,
            StatusCodes.Status200OK,
            """{"ok":true}""",
            null);
        await almacen.ConfirmarAsync();

        await using var segundoContexto = _fixture.CrearContexto();
        var segundoAlmacen = new AlmacenIdempotencia(segundoContexto);
        var duplicado = original with
        {
            Id = Guid.CreateVersion7(),
            HashPayload = new string('f', 64)
        };

        await segundoAlmacen.IniciarTransaccionAsync();
        (await segundoAlmacen.IntentarRegistrarAsync(duplicado)).Should().BeFalse();
        await segundoAlmacen.RevertirAsync();

        await using var verificacion = _fixture.CrearContexto();
        (await verificacion.SolicitudesIdempotencia.CountAsync(solicitud =>
            solicitud.Endpoint == original.Endpoint
            && solicitud.Clave == original.Clave)).Should().Be(1);
    }

    [Fact]
    public async Task Debe_RevertirElRegistro_Cuando_NoSeConfirmaLaTransaccion()
    {
        await using var contexto = _fixture.CrearContexto();
        var almacen = new AlmacenIdempotencia(contexto);
        var registro = CrearRegistro();

        await almacen.IniciarTransaccionAsync();
        (await almacen.IntentarRegistrarAsync(registro)).Should().BeTrue();
        await almacen.RevertirAsync();

        await using var verificacion = _fixture.CrearContexto();
        var recuperado = await new AlmacenIdempotencia(verificacion)
            .ObtenerAsync(registro.Endpoint, registro.Clave);

        recuperado.Should().BeNull();
    }

    private static RegistroIdempotencia CrearRegistro()
    {
        var ahora = DateTimeOffset.UtcNow;

        return new RegistroIdempotencia(
            Guid.CreateVersion7(),
            $"POST /api/v1/becas/ofertas/{Guid.NewGuid():N}",
            Guid.NewGuid().ToString("N"),
            new string('a', 64),
            "EN_PROCESO",
            null,
            null,
            null,
            ahora,
            ahora.AddDays(7));
    }
}
