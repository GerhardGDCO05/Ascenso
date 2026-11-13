using Ascenso.Programas.Domain.Donantes.Agregados;
using Ascenso.Programas.Domain.Donantes.Entidades;
using Ascenso.Programas.Domain.Excepciones;
using Ascenso.Programas.Domain.Interfaces;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Ascenso.Programas.Infrastructure.Persistence;

/// <summary>
/// Contexto de persistencia del microservicio de programas, donantes, convenios y presupuesto.
/// </summary>
public sealed class ProgramasDbContext : DbContext, IUnitOfWork
{
    public const string Esquema = "programas";

    public ProgramasDbContext(DbContextOptions<ProgramasDbContext> options)
        : base(options)
    {
    }

    public DbSet<Donante> Donantes => Set<Donante>();

    public DbSet<AporteDonante> AportesDonante => Set<AporteDonante>();

    public async Task<int> GuardarCambiosAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new ExcepcionConflictoConcurrencia(ex.Message);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException pg
            && pg.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            if (string.Equals(pg.ConstraintName, "ux_donante_identificacion_fiscal", StringComparison.OrdinalIgnoreCase))
            {
                throw new IdentificacionFiscalDuplicadaException(pg.Detail ?? "duplicada");
            }

            if (string.Equals(pg.ConstraintName, "ux_aporte_donante_referencia_bancaria", StringComparison.OrdinalIgnoreCase))
            {
                throw new ReferenciaBancariaDuplicadaException(pg.Detail ?? "duplicada");
            }

            throw;
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema(Esquema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ProgramasDbContext).Assembly);

        modelBuilder.AddInboxStateEntity(entidad => entidad.ToTable("inbox_state"));
        modelBuilder.AddOutboxStateEntity(entidad => entidad.ToTable("outbox_state"));
        modelBuilder.AddOutboxMessageEntity(entidad => entidad.ToTable("outbox_message"));
    }
}

