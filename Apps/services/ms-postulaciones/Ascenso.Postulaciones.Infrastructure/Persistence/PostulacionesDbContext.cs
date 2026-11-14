using Ascenso.Postulaciones.Domain.Excepciones;
using Ascenso.Postulaciones.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Ascenso.Postulaciones.Infrastructure.Persistence;

/// <summary>
/// Contexto de persistencia del microservicio de postulaciones, admisión y matrícula.
/// </summary>
public sealed class PostulacionesDbContext : DbContext, IUnitOfWork
{
    public const string Esquema = "postulaciones";

    public PostulacionesDbContext(DbContextOptions<PostulacionesDbContext> options)
        : base(options)
    {
    }

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
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema(Esquema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PostulacionesDbContext).Assembly);
    }
}
