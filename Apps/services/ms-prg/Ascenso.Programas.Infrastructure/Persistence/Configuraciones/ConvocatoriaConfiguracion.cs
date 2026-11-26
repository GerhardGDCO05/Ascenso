using System.Text.Json;
using Ascenso.Programas.Domain.Convocatorias.Agregados;
using Ascenso.Programas.Domain.Enums;
using Ascenso.Programas.Domain.Programas.Agregados;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Ascenso.Programas.Infrastructure.Persistence.Configuraciones;

internal sealed class ConvocatoriaConfiguracion : IEntityTypeConfiguration<Convocatoria>
{
    public void Configure(EntityTypeBuilder<Convocatoria> builder)
    {
        builder.ToTable("convocatorias", tabla =>
        {
            tabla.HasCheckConstraint(
                "ck_convocatoria_estado_valido",
                $"estado IN ({ConfiguracionPersistencia.ValoresSql<EstadoConvocatoria>()})");
            tabla.HasCheckConstraint("ck_convocatoria_cupos_positivos", "cupos_asignados >= 1");
            tabla.HasCheckConstraint("ck_convocatoria_edad_minima_valida", "criterio_edad_minima BETWEEN 0 AND 120");
            tabla.HasCheckConstraint("ck_convocatoria_edad_maxima_valida", "criterio_edad_maxima BETWEEN 0 AND 120");
            tabla.HasCheckConstraint("ck_convocatoria_edades_ordenadas", "criterio_edad_minima <= criterio_edad_maxima");
            tabla.HasCheckConstraint("ck_convocatoria_promedio_valido", "criterio_promedio_minimo BETWEEN 0 AND 20");
            tabla.HasCheckConstraint("ck_convocatoria_tope_ingreso_no_negativo", "criterio_tope_ingreso_familiar >= 0");
            tabla.HasCheckConstraint("ck_convocatoria_fechas_ordenadas", "fecha_cierre > fecha_apertura");
            tabla.HasCheckConstraint(
                "ck_convocatoria_subsanacion_despues_del_cierre",
                "fecha_limite_subsanacion_documentos >= fecha_cierre");
            tabla.HasCheckConstraint(
                "ck_convocatoria_evaluacion_ordenada",
                "fecha_cierre_evaluacion > fecha_inicio_evaluacion");
            tabla.HasCheckConstraint(
                "ck_convocatoria_evaluacion_despues_de_subsanacion",
                "fecha_cierre_evaluacion > fecha_limite_subsanacion_documentos");
        });

        builder.HasKey(convocatoria => convocatoria.Id).HasName("pk_convocatorias");

        builder.Ignore(convocatoria => convocatoria.EventosDominio);

        ConfiguracionPersistencia.ConfigurarEntidadAuditable(builder);

        builder.Property(convocatoria => convocatoria.ProgramaBecaId)
            .HasColumnName("programa_beca_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(convocatoria => convocatoria.PeriodoAcademicoId)
            .HasColumnName("periodo_academico_id")
            .HasMaxLength(36)
            .IsRequired();

        builder.Property(convocatoria => convocatoria.Codigo)
            .HasColumnName("codigo")
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(convocatoria => convocatoria.Nombre)
            .HasColumnName("nombre")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(convocatoria => convocatoria.FechaApertura)
            .HasColumnName("fecha_apertura")
            .HasColumnType("timestamptz")
            .IsRequired();

        builder.Property(convocatoria => convocatoria.FechaCierre)
            .HasColumnName("fecha_cierre")
            .HasColumnType("timestamptz")
            .IsRequired();

        builder.Property(convocatoria => convocatoria.FechaLimiteSubsanacionDocumentos)
            .HasColumnName("fecha_limite_subsanacion_documentos")
            .HasColumnType("timestamptz")
            .IsRequired();

        builder.Property(convocatoria => convocatoria.FechaInicioEvaluacion)
            .HasColumnName("fecha_inicio_evaluacion")
            .HasColumnType("timestamptz")
            .IsRequired();

        builder.Property(convocatoria => convocatoria.FechaCierreEvaluacion)
            .HasColumnName("fecha_cierre_evaluacion")
            .HasColumnType("timestamptz")
            .IsRequired();

        builder.Property(convocatoria => convocatoria.RequiereRevisionDocumentalPrevia)
            .HasColumnName("requiere_revision_documental_previa")
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(convocatoria => convocatoria.CuposAsignados)
            .HasColumnName("cupos_asignados")
            .IsRequired();

        builder.Property(convocatoria => convocatoria.Estado)
            .HasColumnName("estado")
            .ComoCodigoEnum()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(convocatoria => convocatoria.FechaPublicacion)
            .HasColumnName("fecha_publicacion")
            .HasColumnType("timestamptz");

        var guidListConverter = new ValueConverter<IReadOnlyList<Guid>, string>(
            lista => JsonSerializer.Serialize(lista, (JsonSerializerOptions?)null),
            json => JsonSerializer.Deserialize<Guid[]>(json, (JsonSerializerOptions?)null) ?? Array.Empty<Guid>());

        var guidListComparer = new ValueComparer<IReadOnlyList<Guid>>(
            (izquierda, derecha) => ReferenceEquals(izquierda, derecha)
                || (izquierda != null && derecha != null && izquierda.SequenceEqual(derecha)),
            lista => lista.Aggregate(0, (hash, item) => HashCode.Combine(hash, item.GetHashCode())),
            lista => lista.ToArray());

        builder.OwnsOne(convocatoria => convocatoria.CriteriosElegibilidad, criterios =>
        {
            criterios.Property(valor => valor.PromedioMinimo)
                .HasColumnName("criterio_promedio_minimo")
                .HasPrecision(5, 2)
                .IsRequired();

            criterios.Property(valor => valor.EdadMinima)
                .HasColumnName("criterio_edad_minima")
                .IsRequired();

            criterios.Property(valor => valor.EdadMaxima)
                .HasColumnName("criterio_edad_maxima")
                .IsRequired();

            criterios.Property(valor => valor.TopeIngresoFamiliar)
                .HasColumnName("criterio_tope_ingreso_familiar")
                .HasPrecision(18, 2)
                .IsRequired();

            var universidadesProp = criterios.Property(valor => valor.UniversidadesPermitidasIds)
                .HasColumnName("criterio_universidades_permitidas_ids")
                .HasColumnType("jsonb")
                .HasConversion(guidListConverter);
            universidadesProp.Metadata.SetValueComparer(guidListComparer);

            var carrerasProp = criterios.Property(valor => valor.CarrerasPermitidasIds)
                .HasColumnName("criterio_carreras_permitidas_ids")
                .HasColumnType("jsonb")
                .HasConversion(guidListConverter);
            carrerasProp.Metadata.SetValueComparer(guidListComparer);
        });

        builder.HasMany(convocatoria => convocatoria.DocumentosRequeridos)
            .WithOne()
            .HasForeignKey(documento => documento.ConvocatoriaId)
            .HasConstraintName("fk_documentos_requeridos_convocatoria")
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(convocatoria => convocatoria.DocumentosRequeridos)
            .HasField("_documentosRequeridos")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(convocatoria => convocatoria.Codigo)
            .IsUnique()
            .HasDatabaseName("ux_convocatoria_codigo");

        builder.HasIndex(convocatoria => new { convocatoria.Estado, convocatoria.FechaCierre })
            .HasDatabaseName("ix_convocatoria_estado_fecha_cierre");

        builder.HasIndex(convocatoria => convocatoria.ProgramaBecaId)
            .HasDatabaseName("ix_convocatoria_programa_beca");

        builder.HasOne<ProgramaBeca>()
            .WithMany()
            .HasForeignKey(convocatoria => convocatoria.ProgramaBecaId)
            .HasConstraintName("fk_convocatorias_programa_beca")
            .OnDelete(DeleteBehavior.Restrict);
    }
}
