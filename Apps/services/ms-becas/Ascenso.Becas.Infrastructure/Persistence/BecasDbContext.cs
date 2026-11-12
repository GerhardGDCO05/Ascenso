using Ascenso.Becas.Domain.Otorgamiento.Agregados;
using Ascenso.Becas.Domain.Otorgamiento.Entidades;
using Ascenso.Becas.Infrastructure.Persistence.Modelos;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace Ascenso.Becas.Infrastructure.Persistence;

/// <summary>
/// Contexto de persistencia del microservicio de becas. Agrupa el agregado de
/// otorgamiento, las proyecciones locales y la idempotencia de comandos.
/// </summary>
public sealed class BecasDbContext : DbContext
{
    public const string Esquema = "becas";

    public BecasDbContext(DbContextOptions<BecasDbContext> options)
        : base(options)
    {
    }

    public DbSet<Beca> Becas => Set<Beca>();

    public DbSet<OfertaBeca> OfertasBeca => Set<OfertaBeca>();

    public DbSet<CompromisoBecario> CompromisosBecarios => Set<CompromisoBecario>();

    public DbSet<HistorialEstadoBeca> HistorialEstadosBeca => Set<HistorialEstadoBeca>();

    public DbSet<ProgramaBecaRef> ProgramasBecaRef => Set<ProgramaBecaRef>();

    public DbSet<CostoMatriculaRef> CostosMatriculaRef => Set<CostoMatriculaRef>();

    public DbSet<ConvenioRef> ConveniosRef => Set<ConvenioRef>();

    public DbSet<IdempotencyRequest> SolicitudesIdempotencia => Set<IdempotencyRequest>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema(Esquema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BecasDbContext).Assembly);

        modelBuilder.AddInboxStateEntity(entidad => entidad.ToTable("inbox_state"));
        modelBuilder.AddOutboxStateEntity(entidad => entidad.ToTable("outbox_state"));
        modelBuilder.AddOutboxMessageEntity(entidad => entidad.ToTable("outbox_message"));
    }
}
