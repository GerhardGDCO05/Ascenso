using Ascenso.Becas.Domain.Otorgamiento.Agregados;
using Ascenso.Becas.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Ascenso.Becas.IntegrationTests.Persistencia;

[Collection(ColeccionPostgreSql.Nombre)]
public sealed class HistorialInmutableTests
{
    private readonly PostgreSqlFixture _fixture;

    public HistorialInmutableTests(PostgreSqlFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Debe_PermitirInsertarHistorial_Cuando_LaBecaExiste()
    {
        await using var contexto = _fixture.CrearContexto();
        var beca = await CrearBecaAsync(contexto);
        var correlacionId = Guid.CreateVersion7();
        var usuarioId = Guid.CreateVersion7();
        var fecha = DateTimeOffset.UtcNow;
        var historialId = Guid.CreateVersion7();

        await contexto.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO becas.historial_estado_beca (
                id, beca_id, tipo_entidad, estado_beca_nuevo, fecha_cambio, motivo,
                estado_terminal_irreversible, fecha_creacion, usuario_autor_id, correlacion_id)
            VALUES (
                {historialId}, {beca.Id}, 'BECA', 'ACTIVA', {fecha}, 'Inserción de prueba',
                false, {fecha}, {usuarioId}, {correlacionId})
            """);

        Assert.True(await contexto.HistorialEstadosBeca
            .AsNoTracking()
            .AnyAsync(registro => registro.Id == historialId));
    }

    [Fact]
    public async Task Debe_RechazarUpdateDelHistorial_Cuando_SeIntentaModificarUnRegistro()
    {
        await using var contexto = _fixture.CrearContexto();
        var beca = await CrearBecaAsync(contexto);
        var historialId = await ObtenerHistorialIdAsync(contexto, beca.Id);

        var excepcion = await Assert.ThrowsAsync<PostgresException>(
            () => contexto.Database.ExecuteSqlInterpolatedAsync($"""
                UPDATE becas.historial_estado_beca
                SET motivo = 'Intento de modificación'
                WHERE id = {historialId}
                """));

        Assert.Equal(PostgresErrorCodes.IntegrityConstraintViolation, excepcion.SqlState);
        Assert.Contains("inmutable", excepcion.MessageText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Debe_RechazarDeleteDelHistorial_Cuando_SeIntentaEliminarUnRegistro()
    {
        await using var contexto = _fixture.CrearContexto();
        var beca = await CrearBecaAsync(contexto);
        var historialId = await ObtenerHistorialIdAsync(contexto, beca.Id);

        var excepcion = await Assert.ThrowsAsync<PostgresException>(
            () => contexto.Database.ExecuteSqlInterpolatedAsync($"""
                DELETE FROM becas.historial_estado_beca
                WHERE id = {historialId}
                """));

        Assert.Equal(PostgresErrorCodes.IntegrityConstraintViolation, excepcion.SqlState);
        Assert.Contains("inmutable", excepcion.MessageText, StringComparison.OrdinalIgnoreCase);
    }

    private static async Task<Beca> CrearBecaAsync(BecasDbContext contexto)
    {
        var beca = Beca.CrearOtorgada(
            DatosPrueba.CrearDatosBeca(),
            DatosPrueba.CrearContextoOperacion());

        contexto.Becas.Add(beca);
        await contexto.SaveChangesAsync();

        return beca;
    }

    private static Task<Guid> ObtenerHistorialIdAsync(
        BecasDbContext contexto,
        Guid becaId)
        => contexto.HistorialEstadosBeca
            .AsNoTracking()
            .Where(registro => registro.BecaId == becaId)
            .Select(registro => registro.Id)
            .FirstAsync();
}
