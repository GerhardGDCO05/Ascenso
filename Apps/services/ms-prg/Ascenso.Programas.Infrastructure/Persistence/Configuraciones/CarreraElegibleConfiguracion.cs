using Ascenso.Programas.Domain.Universidades.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ascenso.Programas.Infrastructure.Persistence.Configuraciones;

internal sealed class CarreraElegibleConfiguracion : IEntityTypeConfiguration<CarreraElegible>
{
    public void Configure(EntityTypeBuilder<CarreraElegible> builder)
    {
        builder.ToTable("carreras_elegibles", tabla =>
        {
            tabla.HasCheckConstraint(
                "ck_carrera_elegible_duracion_positiva",
                "duracion_semestres > 0");
        });

        builder.HasKey(carrera => carrera.Id).HasName("pk_carreras_elegibles");

        ConfiguracionPersistencia.ConfigurarEntidadAuditable(builder);

        builder.Property(carrera => carrera.UniversidadAliadaId)
            .HasColumnName("universidad_aliada_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(carrera => carrera.CodigoCarrera)
            .HasColumnName("codigo_carrera")
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(carrera => carrera.Nombre)
            .HasColumnName("nombre")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(carrera => carrera.DuracionSemestres)
            .HasColumnName("duracion_semestres")
            .IsRequired();

        builder.Property(carrera => carrera.Activa)
            .HasColumnName("activa")
            .IsRequired();

        builder.HasMany(carrera => carrera.CostosMatricula)
            .WithOne()
            .HasForeignKey(costo => costo.CarreraElegibleId)
            .HasConstraintName("fk_costos_matricula_carrera")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Navigation(carrera => carrera.CostosMatricula)
            .HasField("_costosMatricula")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(carrera => new { carrera.UniversidadAliadaId, carrera.CodigoCarrera })
            .IsUnique()
            .HasDatabaseName("ux_carrera_elegible_universidad_codigo");
    }
}
