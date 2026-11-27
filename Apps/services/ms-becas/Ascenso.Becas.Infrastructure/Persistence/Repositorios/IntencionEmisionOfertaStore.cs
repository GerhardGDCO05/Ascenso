using System.Text.Json;
using Ascenso.Becas.Application.Otorgamiento.Puertos;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using NpgsqlTypes;

namespace Ascenso.Becas.Infrastructure.Persistence.Repositorios;

/// <summary>
/// La primera escritura usa otra conexión para sobrevivir a la transacción
/// ambiental de idempotencia. La eliminación usa el DbContext de la beca.
/// </summary>
public sealed class IntencionEmisionOfertaStore : IIntencionEmisionOfertaStore
{
    private static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);
    private readonly BecasDbContext _contexto;
    private readonly string _cadenaConexion;

    public IntencionEmisionOfertaStore(BecasDbContext contexto)
    {
        _contexto = contexto ?? throw new ArgumentNullException(nameof(contexto));
        _cadenaConexion = contexto.Database.GetConnectionString()
            ?? throw new InvalidOperationException("Falta la conexión de MS-BECA.");
    }

    public async Task GuardarAsync(IntencionEmisionOferta intencion,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(intencion);
        await using var conexion = new NpgsqlConnection(_cadenaConexion);
        await conexion.OpenAsync(cancellationToken);
        await using var comando = new NpgsqlCommand("""
            INSERT INTO becas.intencion_emision_oferta
                (oferta_id, postulacion_id, payload, creado_en)
            VALUES (@oferta, @postulacion, @payload, @creado)
            """, conexion);
        comando.Parameters.AddWithValue("oferta", intencion.OfertaId);
        comando.Parameters.AddWithValue("postulacion", intencion.PostulacionId);
        comando.Parameters.AddWithValue("payload", NpgsqlDbType.Jsonb,
            JsonSerializer.Serialize(intencion, _json));
        comando.Parameters.AddWithValue("creado", DateTimeOffset.UtcNow);
        await comando.ExecuteNonQueryAsync(cancellationToken);
    }

    public Task<IntencionEmisionOferta?> ObtenerAsync(Guid ofertaId,
        CancellationToken cancellationToken = default)
        => LeerAsync("oferta_id", ofertaId, cancellationToken);

    public Task<IntencionEmisionOferta?> ObtenerPorPostulacionAsync(Guid postulacionId,
        CancellationToken cancellationToken = default)
        => LeerAsync("postulacion_id", postulacionId, cancellationToken);

    public async Task<IReadOnlyList<Guid>> ListarPendientesAsync(int limite,
        Guid? despuesDe = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(limite);
        await using var conexion = new NpgsqlConnection(_cadenaConexion);
        await conexion.OpenAsync(cancellationToken);
        await using var comando = new NpgsqlCommand("""
            SELECT oferta_id FROM becas.intencion_emision_oferta
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
                "La intención debe eliminarse junto con la confirmación local.");
        await _contexto.Database.ExecuteSqlInterpolatedAsync(
            $"DELETE FROM becas.intencion_emision_oferta WHERE oferta_id = {ofertaId}",
            cancellationToken);
    }

    private async Task<IntencionEmisionOferta?> LeerAsync(string columna, Guid id,
        CancellationToken cancellationToken)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("El identificador es obligatorio.", nameof(id));
        await using var conexion = new NpgsqlConnection(_cadenaConexion);
        await conexion.OpenAsync(cancellationToken);
        await using var comando = new NpgsqlCommand(
            $"SELECT payload FROM becas.intencion_emision_oferta WHERE {columna} = @id",
            conexion);
        comando.Parameters.AddWithValue("id", id);
        var payload = (string?)await comando.ExecuteScalarAsync(cancellationToken);
        return payload is null
            ? null
            : JsonSerializer.Deserialize<IntencionEmisionOferta>(payload, _json);
    }
}
