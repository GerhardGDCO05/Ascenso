using Ascenso.Becas.Domain.Enums;
using Ascenso.Becas.Domain.Otorgamiento.Agregados;
using Ascenso.Becas.Domain.Ports;
using Microsoft.EntityFrameworkCore;

namespace Ascenso.Becas.Infrastructure.Persistence.Repositorios;

/// <summary>
/// Implementación EF Core del puerto del agregado <c>Beca</c>. Las entidades
/// hijas y los tipos propiedad del agregado se cargan de forma explícita o
/// automática para reconstruirlo completo.
/// </summary>
public sealed class BecaRepository : IBecaRepository
{
    private readonly BecasDbContext _contexto;

    public BecaRepository(BecasDbContext contexto)
    {
        _contexto = contexto ?? throw new ArgumentNullException(nameof(contexto));
    }

    public Task<Beca?> ObtenerPorIdAsync(Guid becaId, CancellationToken cancellationToken = default)
    {
        if (becaId == Guid.Empty)
        {
            throw new ArgumentException("El identificador de la beca es obligatorio.", nameof(becaId));
        }

        return _contexto.Becas
            .Include(beca => beca.Oferta)
            .Include(beca => beca.Compromiso)
            .Include(beca => beca.Historial)
            .AsSplitQuery()
            .SingleOrDefaultAsync(beca => beca.Id == becaId, cancellationToken);
    }

    public async Task AgregarAsync(Beca beca, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(beca);
        await _contexto.Becas.AddAsync(beca, cancellationToken);
    }

    public async Task<Beca?> ObtenerBecaParaActualizarAsync(
        Guid becaId,
        CancellationToken cancellationToken = default)
    {
        if (becaId == Guid.Empty)
        {
            throw new ArgumentException("El identificador de la beca es obligatorio.", nameof(becaId));
        }

        ExigirTransaccionActiva();

        await _contexto.Database.ExecuteSqlInterpolatedAsync(
            $"""
            SELECT 1
            FROM becas.beca
            WHERE id = {becaId}
            FOR UPDATE
            """,
            cancellationToken);

        return await ObtenerPorIdAsync(becaId, cancellationToken);
    }

    public async Task AdquirirBloqueoEstudianteAsync(
        Guid estudianteId,
        CancellationToken cancellationToken = default)
    {
        if (estudianteId == Guid.Empty)
        {
            throw new ArgumentException("El identificador del estudiante es obligatorio.", nameof(estudianteId));
        }

        ExigirTransaccionActiva();

        var estudiante = estudianteId.ToString("D");
        await _contexto.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock(hashtextextended({estudiante}, 0))",
            cancellationToken);
    }

    public Task<bool> ExisteAsync(Guid becaId, CancellationToken cancellationToken = default)
        => _contexto.Becas
            .AsNoTracking()
            .AnyAsync(beca => beca.Id == becaId, cancellationToken);

    public Task<bool> ExisteActivaConCoberturaTotalAsync(
        Guid estudianteId,
        CancellationToken cancellationToken = default)
        => _contexto.Becas
            .AsNoTracking()
            .AnyAsync(
                beca => beca.PostulanteEstudianteId == estudianteId
                    && beca.Estado == EstadoBeca.Activa
                    && beca.CoberturaAplicada.Modalidad == TipoCobertura.Total,
                cancellationToken);

    public Task<bool> ExisteActivaEnPeriodoAsync(
        Guid estudianteId,
        Guid periodoAcademicoId,
        CancellationToken cancellationToken = default)
        => _contexto.Becas
            .AsNoTracking()
            .AnyAsync(
                beca => beca.PostulanteEstudianteId == estudianteId
                    && beca.PeriodoVigenteActualId == periodoAcademicoId
                    && beca.Estado == EstadoBeca.Activa,
                cancellationToken);

    private void ExigirTransaccionActiva()
    {
        if (_contexto.Database.CurrentTransaction is null)
        {
            throw new InvalidOperationException(
                "La operación requiere una transacción activa para mantener el bloqueo hasta el commit.");
        }
    }
}
