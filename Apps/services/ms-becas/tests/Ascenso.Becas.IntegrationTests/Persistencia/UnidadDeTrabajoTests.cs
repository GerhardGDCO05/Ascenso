using System.Data;
using Ascenso.Becas.Application.Idempotencia;
using Ascenso.Becas.Domain.Otorgamiento.Agregados;
using Ascenso.Becas.Domain.Ports;
using Ascenso.Becas.Infrastructure.Persistence.Repositorios;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using UnidadDeTrabajoEf = Ascenso.Becas.Infrastructure.Persistence.UnidadDeTrabajo.UnidadDeTrabajo;

namespace Ascenso.Becas.IntegrationTests.Persistencia;

[Collection(ColeccionPostgreSql.Nombre)]
public sealed class UnidadDeTrabajoTests
{
    private readonly PostgreSqlFixture _fixture;

    public UnidadDeTrabajoTests(PostgreSqlFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Debe_ConfirmarLosCambios_Cuando_LaOperacionTermina()
    {
        await using var contexto = _fixture.CrearContexto();
        var unidad = new UnidadDeTrabajoEf(contexto);
        var beca = CrearBeca();

        await unidad.EjecutarEnTransaccionAsync(
            async token =>
            {
                contexto.Becas.Add(beca);
                await unidad.GuardarCambiosAsync(token);
            });

        contexto.Database.CurrentTransaction.Should().BeNull();

        await using var verificacion = _fixture.CrearContexto();
        (await verificacion.Becas.FindAsync(beca.Id)).Should().NotBeNull();
    }

    [Fact]
    public async Task Debe_RevertirLosCambios_Cuando_LaOperacionFalla()
    {
        await using var contexto = _fixture.CrearContexto();
        var unidad = new UnidadDeTrabajoEf(contexto);
        var beca = CrearBeca();

        var accion = () => unidad.EjecutarEnTransaccionAsync(
            async token =>
            {
                contexto.Becas.Add(beca);
                await unidad.GuardarCambiosAsync(token);
                throw new InvalidOperationException("fallo simulado");
            });

        await accion.Should().ThrowAsync<InvalidOperationException>();

        contexto.Database.CurrentTransaction.Should().BeNull();

        await using var verificacion = _fixture.CrearContexto();
        (await verificacion.Becas.FindAsync(beca.Id)).Should().BeNull();
    }

    [Fact]
    public async Task Debe_UsarAislamientoSerializable_Cuando_SeSolicita()
    {
        await using var contexto = _fixture.CrearContexto();
        var unidad = new UnidadDeTrabajoEf(contexto);
        IsolationLevel? nivelObservado = null;

        await unidad.EjecutarEnTransaccionAsync(
            _ =>
            {
                nivelObservado = contexto.Database.CurrentTransaction?
                    .GetDbTransaction()
                    .IsolationLevel;
                return Task.CompletedTask;
            },
            NivelAislamientoTransaccion.Serializable);

        nivelObservado.Should().Be(IsolationLevel.Serializable);
    }

    [Fact]
    public async Task Debe_UsarAislamientoReadCommitted_Cuando_NoSeEspecificaNivel()
    {
        await using var contexto = _fixture.CrearContexto();
        var unidad = new UnidadDeTrabajoEf(contexto);
        IsolationLevel? nivelObservado = null;

        await unidad.EjecutarEnTransaccionAsync(
            _ =>
            {
                nivelObservado = contexto.Database.CurrentTransaction?
                    .GetDbTransaction()
                    .IsolationLevel;
                return Task.CompletedTask;
            });

        nivelObservado.Should().Be(IsolationLevel.ReadCommitted);
    }

    [Fact]
    public async Task Debe_ReutilizarLaTransaccionAmbiental_Cuando_LaIdempotenciaYaLaAbrio()
    {
        await using var contexto = _fixture.CrearContexto();
        var almacen = new AlmacenIdempotencia(contexto);
        var unidad = new UnidadDeTrabajoEf(contexto);
        var beca = CrearBeca();
        var registro = CrearRegistroIdempotencia();

        await almacen.IniciarTransaccionAsync();
        (await almacen.IntentarRegistrarAsync(registro)).Should().BeTrue();
        var transaccionAmbiental = contexto.Database.CurrentTransaction;

        await unidad.EjecutarEnTransaccionAsync(
            async token =>
            {
                contexto.Database.CurrentTransaction.Should().BeSameAs(transaccionAmbiental);
                contexto.Becas.Add(beca);
                await unidad.GuardarCambiosAsync(token);
            },
            NivelAislamientoTransaccion.Serializable);

        contexto.Database.CurrentTransaction.Should().BeSameAs(transaccionAmbiental);

        await almacen.ConfirmarAsync();

        contexto.Database.CurrentTransaction.Should().BeNull();

        await using var verificacion = _fixture.CrearContexto();
        (await verificacion.Becas.FindAsync(beca.Id)).Should().NotBeNull();
        (await verificacion.SolicitudesIdempotencia
            .AnyAsync(solicitud =>
                solicitud.Endpoint == registro.Endpoint
                && solicitud.Clave == registro.Clave))
            .Should().BeTrue();
    }

    [Fact]
    public async Task Debe_RevertirConjuntamente_Cuando_LaIdempotenciaSeRevierte()
    {
        await using var contexto = _fixture.CrearContexto();
        var almacen = new AlmacenIdempotencia(contexto);
        var unidad = new UnidadDeTrabajoEf(contexto);
        var beca = CrearBeca();
        var registro = CrearRegistroIdempotencia();

        await almacen.IniciarTransaccionAsync();
        (await almacen.IntentarRegistrarAsync(registro)).Should().BeTrue();

        await unidad.EjecutarEnTransaccionAsync(
            async token =>
            {
                contexto.Becas.Add(beca);
                await unidad.GuardarCambiosAsync(token);
            });

        await almacen.RevertirAsync();

        contexto.Database.CurrentTransaction.Should().BeNull();

        await using var verificacion = _fixture.CrearContexto();
        (await verificacion.Becas.FindAsync(beca.Id)).Should().BeNull();
        (await verificacion.SolicitudesIdempotencia
            .AnyAsync(solicitud =>
                solicitud.Endpoint == registro.Endpoint
                && solicitud.Clave == registro.Clave))
            .Should().BeFalse();
    }

    private static Beca CrearBeca()
    {
        var datos = DatosPrueba.CrearDatosBeca();
        return Beca.CrearOtorgada(datos, DatosPrueba.CrearContextoOperacion());
    }

    private static RegistroIdempotencia CrearRegistroIdempotencia()
    {
        var ahora = DateTimeOffset.UtcNow;

        return new RegistroIdempotencia(
            Guid.CreateVersion7(),
            $"POST /api/v1/becas/ofertas/{Guid.NewGuid():N}",
            Guid.NewGuid().ToString("N"),
            new string('d', 64),
            "EN_PROCESO",
            null,
            null,
            null,
            ahora,
            ahora.AddDays(7));
    }
}
