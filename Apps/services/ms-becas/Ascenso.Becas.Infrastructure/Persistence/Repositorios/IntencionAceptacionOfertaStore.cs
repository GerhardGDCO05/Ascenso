using System.Text.Json;
using Ascenso.Becas.Application.Otorgamiento.Puertos;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using NpgsqlTypes;

namespace Ascenso.Becas.Infrastructure.Persistence.Repositorios;

/// <summary>
/// Escribe en conexión independiente de la transacción de la petición y
/// elimina la intención junto con la Beca, el calendario y el Outbox.
/// </summary>
public sealed class IntencionAceptacionOfertaStore : IIntencionAceptacionOfertaStore
{
    private static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);
    private readonly BecasDbContext _contexto;
    private readonly string _cadenaConexion;

    public IntencionAceptacionOfertaStore(BecasDbContext contexto)
    {
        _contexto = contexto ?? throw new ArgumentNullException(nameof(contexto));
        _cadenaConexion = contexto.Database.GetConnectionString()
            ?? throw new InvalidOperationException("Falta la conexión de MS-BECA.");
    }

    public async Task GuardarAsync(IntencionAceptacionOferta intencion,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(intencion);
        await using var conexion = new NpgsqlConnection(_cadenaConexion);
        await conexion.OpenAsync(cancellationToken);
        await using var comando = new NpgsqlCommand("""
            INSERT INTO becas.intencion_aceptacion_oferta
                (oferta_id, beca_id, payload, creado_en)
            VALUES (@oferta, @beca, @payload, @creado)
            ON CONFLICT (oferta_id) DO NOTHING
            """, conexion);
        comando.Parameters.AddWithValue("oferta", intencion.OfertaId);
        comando.Parameters.AddWithValue("beca", intencion.BecaId);
        comando.Parameters.AddWithValue("payload", NpgsqlDbType.Jsonb,
            JsonSerializer.Serialize(intencion, _json));
        comando.Parameters.AddWithValue("creado", DateTimeOffset.UtcNow);
        if (await comando.ExecuteNonQueryAsync(cancellationToken) == 0)
            throw new InvalidOperationException(
                "La oferta ya tiene una intención de aceptación pendiente.");
    }

    public async Task<IntencionAceptacionOferta?> ObtenerAsync(Guid ofertaId,
        CancellationToken cancellationToken = default)
    {
        if (ofertaId == Guid.Empty)
            throw new ArgumentException("La oferta es obligatoria.", nameof(ofertaId));
        await using var conexion = new NpgsqlConnection(_cadenaConexion);
        await conexion.OpenAsync(cancellationToken);
        await using var comando = new NpgsqlCommand("""
            SELECT payload FROM becas.intencion_aceptacion_oferta
            WHERE oferta_id = @oferta
            """, conexion);
        comando.Parameters.AddWithValue("oferta", ofertaId);
        var payload = (string?)await comando.ExecuteScalarAsync(cancellationToken);
        return payload is null
            ? null
            : JsonSerializer.Deserialize<IntencionAceptacionOferta>(payload, _json);
    }

    public async Task<IReadOnlyList<Guid>> ListarPendientesAsync(int limite,
        Guid? despuesDe = null, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(limite);
        await using var conexion = new NpgsqlConnection(_cadenaConexion);
        await conexion.OpenAsync(cancellationToken);
        await using var comando = new NpgsqlCommand("""
            SELECT oferta_id FROM becas.intencion_aceptacion_oferta
            WHERE @despues IS NULL OR oferta_id > @despues
            ORDER BY oferta_id LIMIT @limite
            """, conexion);
        comando.Parameters.AddWithValue("limite", limite);
        comando.Parameters.AddWithValue("despues", NpgsqlDbType.Uuid,
            despuesDe is null ? DBNull.Value : despuesDe.Value);
        await using var lector = await comando.ExecuteReaderAsync(cancellationToken);
        var ids = new List<Guid>();
        while (await lector.ReadAsync(cancellationToken))
            ids.Add(lector.GetGuid(0));
        return ids;
    }

    public async Task EliminarEnTransaccionAsync(Guid ofertaId,
        CancellationToken cancellationToken = default)
    {
        if (_contexto.Database.CurrentTransaction is null)
            throw new InvalidOperationException(
                "La intención debe eliminarse junto con la aceptación local.");
        await _contexto.Database.ExecuteSqlInterpolatedAsync(
            $"DELETE FROM becas.intencion_aceptacion_oferta WHERE oferta_id = {ofertaId}",
            cancellationToken);
    }
}
