using Ascenso.Postulaciones.Domain.Comites.Agregados;
using Ascenso.Postulaciones.Domain.Comites.Entidades;
using Ascenso.Postulaciones.Domain.Excepciones;
using Ascenso.Postulaciones.Domain.Interfaces;
using Ascenso.Postulaciones.Domain.Postulantes.Agregados;
using Ascenso.Postulaciones.Domain.Postulantes.Entidades;
using Ascenso.Postulaciones.Domain.Rubricas.Agregados;
using Ascenso.Postulaciones.Domain.Rubricas.Entidades;
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

    public DbSet<Postulante> Postulantes => Set<Postulante>();

    public DbSet<MiembroFamiliar> MiembrosFamiliares => Set<MiembroFamiliar>();

    public DbSet<RubricaEvaluacion> Rubricas => Set<RubricaEvaluacion>();

    public DbSet<CriterioEvaluacion> CriteriosEvaluacion => Set<CriterioEvaluacion>();

    public DbSet<ComiteEvaluador> ComitesEvaluadores => Set<ComiteEvaluador>();

    public DbSet<MiembroComite> MiembrosComite => Set<MiembroComite>();

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
        catch (DbUpdateException ex)
        {
            var excepcionTraducida = TraductorExcepcionesPersistencia.TraducirSiAplica(ex);
            if (excepcionTraducida is not null)
            {
                throw excepcionTraducida;
            }

            throw;
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
