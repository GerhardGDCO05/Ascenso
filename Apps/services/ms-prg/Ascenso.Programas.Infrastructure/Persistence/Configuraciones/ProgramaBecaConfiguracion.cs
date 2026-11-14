using System.Text.Json;
using Ascenso.Programas.Domain.Enums;
using Ascenso.Programas.Domain.Programas.Agregados;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Ascenso.Programas.Infrastructure.Persistence.Configuraciones;

internal sealed class ProgramaBecaConfiguracion : IEntityTypeConfiguration<ProgramaBeca>
{
    public void Configure(EntityTypeBuilder<ProgramaBeca> builder)
    {
        builder.ToTable("programas_beca", tabla =>
        {
            tabla.HasCheckConstraint(
                "ck_programa_beca_estado_valido",
                $"estado IN ({ConfiguracionPersistencia.ValoresSql<EstadoProgramaBeca>()})");
            tabla.HasCheckConstraint("ck_programa_beca_cupo_positivo", "cupo_total > 0");
            tabla.HasCheckConstraint("ck_programa_beca_cupos_ocupados_no_negativo", "cupos_ocupados >= 0");
            tabla.HasCheckConstraint("ck_programa_beca_duracion_positiva", "duracion_periodos > 0");
            tabla.HasCheckConstraint("ck_programa_beca_fechas_vigencia", "vigencia_hasta > vigencia_desde");
            tabla.HasCheckConstraint("ck_programa_beca_presupuesto_no_negativo", "presupuesto_total_asignado >= 0");
        });

        builder.HasKey(programa => programa.Id).HasName("pk_programas_beca");

        builder.Ignore(programa => programa.EventosDominio);

        ConfiguracionPersistencia.ConfigurarEntidadAuditable(builder);

        builder.Property(programa => programa.Codigo)
            .HasColumnName("codigo")
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(programa => programa.Nombre)
            .HasColumnName("nombre")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(programa => programa.Descripcion)
            .HasColumnName("descripcion")
            .HasMaxLength(1500)
            .IsRequired();

        builder.OwnsOne(programa => programa.PoliticaCobertura, politica =>
        {
            politica.Property(valor => valor.Modalidad)
                .HasColumnName("modalidad_cobertura")
                .ComoCodigoEnum()
                .HasMaxLength(20)
                .IsRequired();

            politica.Property(valor => valor.PorcentajeCobertura)
                .ComoDinero("porcentaje_cobertura")
                .IsRequired();

            politica.Property(valor => valor.MontoFijoPeriodo)
                .ComoDinero("monto_fijo_periodo")
                .IsRequired();

            politica.Property(valor => valor.Moneda)
                .HasColumnName("moneda_cobertura")
                .HasMaxLength(3)
                .IsRequired();
        });

        builder.Property(programa => programa.CupoTotal)
            .HasColumnName("cupo_total")
            .IsRequired();

        builder.Property(programa => programa.CuposOcupados)
            .HasColumnName("cupos_ocupados")
            .IsRequired();

        builder.Property(programa => programa.DuracionPeriodos)
            .HasColumnName("duracion_periodos")
            .IsRequired();

        builder.Property(programa => programa.PeriodoInicialId)
            .HasColumnName("periodo_inicial_id")
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(programa => programa.VigenciaDesde)
            .HasColumnName("vigencia_desde")
            .HasColumnType("date")
            .IsRequired();

        builder.Property(programa => programa.VigenciaHasta)
            .HasColumnName("vigencia_hasta")
            .HasColumnType("date")
            .IsRequired();

        builder.Property(programa => programa.PermiteAcumulacion)
            .HasColumnName("permite_acumulacion")
            .IsRequired();

        builder.Property(programa => programa.RequiereCompromisoFirmado)
            .HasColumnName("requiere_compromiso_firmado")
            .IsRequired();

        builder.Property(programa => programa.TerminosCompromisoEspecial)
            .HasColumnName("terminos_compromiso_especial")
            .HasMaxLength(2000);

        builder.Property(programa => programa.PresupuestoTotalAsignado)
            .ComoDinero("presupuesto_total_asignado")
            .IsRequired();

        builder.Property(programa => programa.Moneda)
            .HasColumnName("moneda")
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(programa => programa.Estado)
            .HasColumnName("estado")
            .ComoCodigoEnum()
            .HasMaxLength(20)
            .IsRequired();

        var guidListConverter = new ValueConverter<IReadOnlyList<Guid>, string>(
            lista => JsonSerializer.Serialize(lista, (JsonSerializerOptions?)null),
            json => JsonSerializer.Deserialize<Guid[]>(json, (JsonSerializerOptions?)null) ?? Array.Empty<Guid>());

        var guidListComparer = new ValueComparer<IReadOnlyList<Guid>>(
            (izquierda, derecha) => ReferenceEquals(izquierda, derecha)
                || (izquierda != null && derecha != null && izquierda.SequenceEqual(derecha)),
            lista => lista.Aggregate(0, (hash, item) => HashCode.Combine(hash, item.GetHashCode())),
            lista => lista.ToArray());

        builder.OwnsOne(programa => programa.CriteriosElegibilidad, criterios =>
        {
            criterios.Property(valor => valor.PromedioMinimoIngreso)
                .HasColumnName("criterio_promedio_minimo_ingreso")
                .HasPrecision(5, 2);

            criterios.Property(valor => valor.EdadMinima)
                .HasColumnName("criterio_edad_minima");

            criterios.Property(valor => valor.EdadMaxima)
                .HasColumnName("criterio_edad_maxima");

            criterios.Property(valor => valor.TopeIngresoFamiliarMensual)
                .HasColumnName("criterio_tope_ingreso_familiar")
                .HasPrecision(18, 2);

            var universidadesProp = criterios.Property(valor => valor.UniversidadesElegiblesIds)
                .HasColumnName("criterio_universidades_elegibles_ids")
                .HasColumnType("jsonb")
                .HasConversion(guidListConverter);
            universidadesProp.Metadata.SetValueComparer(guidListComparer);

            var carrerasProp = criterios.Property(valor => valor.CarrerasElegiblesIds)
                .HasColumnName("criterio_carreras_elegibles_ids")
                .HasColumnType("jsonb")
                .HasConversion(guidListConverter);
            carrerasProp.Metadata.SetValueComparer(guidListComparer);
        });

        builder.OwnsOne(programa => programa.CondicionesPermanencia, condiciones =>
        {
            condiciones.Property(valor => valor.PromedioMinimoRenovacion)
                .HasColumnName("permanencia_promedio_minimo_renovacion")
                .HasPrecision(5, 2);

            condiciones.Property(valor => valor.MateriasMinimasAprobadas)
                .HasColumnName("permanencia_materias_minimas_aprobadas");

            condiciones.Property(valor => valor.MaximoMateriasReprobadas)
                .HasColumnName("permanencia_maximo_materias_reprobadas");

            condiciones.Property(valor => valor.HorasServicioPorPeriodo)
                .HasColumnName("permanencia_horas_servicio_periodo");

            condiciones.Property(valor => valor.FrecuenciaReportes)
                .HasColumnName("permanencia_frecuencia_reportes")
                .ComoCodigoEnum()
                .HasMaxLength(20);

            condiciones.Property(valor => valor.ConsecuenciaIncumplimiento)
                .HasColumnName("permanencia_consecuencia_incumplimiento")
                .ComoCodigoEnum()
                .HasMaxLength(30);

            condiciones.Property(valor => valor.MargenRiesgoPromedio)
                .HasColumnName("permanencia_margen_riesgo_promedio")
                .HasPrecision(5, 2);

            condiciones.Property(valor => valor.DuracionMaximaPausaPeriodos)
                .HasColumnName("permanencia_duracion_maxima_pausa_periodos");

            condiciones.Property(valor => valor.PermiteRenovacionTardiaExcepcional)
                .HasColumnName("permanencia_permite_renovacion_tardia");
        });

        builder.HasMany(programa => programa.AsignacionesFondo)
            .WithOne()
            .HasForeignKey(asignacion => asignacion.ProgramaBecaId)
            .HasConstraintName("fk_asignaciones_fondo_programa_programa")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Navigation(programa => programa.AsignacionesFondo)
            .HasField("_asignacionesFondo")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(programa => programa.Codigo)
            .IsUnique()
            .HasDatabaseName("ux_programa_beca_codigo");
    }
}
