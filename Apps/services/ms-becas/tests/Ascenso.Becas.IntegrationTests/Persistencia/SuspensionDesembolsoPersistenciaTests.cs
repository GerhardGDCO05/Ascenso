using Ascenso.Becas.Domain.Enums;
using Ascenso.Becas.Domain.Otorgamiento.ValueObjects;
using Ascenso.Becas.Infrastructure.Persistence.Repositorios;

namespace Ascenso.Becas.IntegrationTests.Persistencia;

[Collection(ColeccionPostgreSql.Nombre)]
public sealed class SuspensionDesembolsoPersistenciaTests
{
    private readonly PostgreSqlFixture _fixture;

    public SuspensionDesembolsoPersistenciaTests(PostgreSqlFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task ConservaSuspensionIndefinidaComoHijaDeBeca()
    {
        await using var db = _fixture.CrearContexto();
        var beca = DatosPrueba.CrearBecaActiva(DatosPrueba.CrearDatosBeca(),
            DatosPrueba.CrearContextoOperacion());
        db.Becas.Add(beca);
        var baseContexto = DatosPrueba.CrearContextoOperacion();
        var contexto = new ContextoOperacion(baseContexto.UsuarioAutorId,
            baseContexto.CorrelacionId, baseContexto.Fecha, "Motivo documentado");
        var suspension = beca.Suspender(TipoSuspensionBeca.Manual,
            "Motivo documentado", "Presentar constancia",
            new DateOnly(2026, 3, 1), null, true, contexto);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var recargada = await new BecaRepository(db).ObtenerPorIdAsync(beca.Id);
        Assert.NotNull(recargada);
        var registrada = Assert.Single(recargada.Suspensiones);
        Assert.Equal(suspension.Id, registrada.Id);
        Assert.True(registrada.EsIndefinida);
        Assert.Equal(EstadoSuspension.Vigente, registrada.Estado);

        recargada.Reactivar(new ContextoOperacion(baseContexto.UsuarioAutorId,
            Guid.CreateVersion7(), baseContexto.Fecha.AddDays(1),
            "Reactivación autorizada por coordinación"));
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var reactivada = await new BecaRepository(db).ObtenerPorIdAsync(beca.Id);
        Assert.NotNull(reactivada);
        Assert.Equal(EstadoBeca.Activa, reactivada.Estado);
        var levantada = Assert.Single(reactivada.Suspensiones);
        Assert.Equal(EstadoSuspension.Levantada, levantada.Estado);
        Assert.NotNull(levantada.FechaResolucion);
        Assert.Equal(baseContexto.UsuarioAutorId, levantada.LevantadaPorCoordinadorId);
    }
}
