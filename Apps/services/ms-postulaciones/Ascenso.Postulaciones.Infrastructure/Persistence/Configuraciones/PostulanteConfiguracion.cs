using Ascenso.Postulaciones.Domain.Enums;
using Ascenso.Postulaciones.Domain.Postulantes.Agregados;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ascenso.Postulaciones.Infrastructure.Persistence.Configuraciones;

/// <summary>
/// Configuración de persistencia EF Core para el agregado <see cref="Postulante"/>.
/// </summary>
internal sealed class PostulanteConfiguracion : IEntityTypeConfiguration<Postulante>
{
    public void Configure(EntityTypeBuilder<Postulante> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("postulantes", tabla =>
        {
            tabla.HasCheckConstraint(
                "ck_postulante_condicion_academica_valida",
                $"condicion_academica IN ({ConfiguracionPersistencia.ValoresSql<CondicionAcademicaIngreso>()})");
            tabla.HasCheckConstraint(
                "ck_postulante_estado_perfil_valido",
                $"estado_perfil IN ({ConfiguracionPersistencia.ValoresSql<EstadoPerfil>()})");
            tabla.HasCheckConstraint(
                "ck_postulante_acepta_tratamiento_datos",
                "acepta_tratamiento_datos = true");
            tabla.HasCheckConstraint(
                "ck_postulante_longitudes_minimas",
                """
                char_length(trim(numero_documento_identidad)) >= 5
                AND char_length(trim(nombres)) >= 2
                AND char_length(trim(apellidos)) >= 2
                """);
            tabla.HasCheckConstraint(
                "ck_postulante_estado_civil_valido",
                $"estado_civil IS NULL OR estado_civil IN ({ConfiguracionPersistencia.ValoresSql<EstadoCivil>()})");
            tabla.HasCheckConstraint(
                "ck_postulante_ocupacion_valida",
                $"ocupacion IS NULL OR ocupacion IN ({ConfiguracionPersistencia.ValoresSql<Ocupacion>()})");
            tabla.HasCheckConstraint(
                "ck_postulante_promedio_actual_rango",
                "promedio_actual IS NULL OR (promedio_actual >= 0 AND promedio_actual <= 20)");
            tabla.HasCheckConstraint(
                "ck_postulante_ingreso_mensual_no_negativo",
                "ingreso_mensual_postulante IS NULL OR ingreso_mensual_postulante >= 0");
            tabla.HasCheckConstraint(
                "ck_postulante_ingreso_familiar_total_no_negativo",
                "ingreso_familiar_mensual_total IS NULL OR ingreso_familiar_mensual_total >= 0");
            tabla.HasCheckConstraint(
                "ck_postulante_numero_integrantes_familia",
                "numero_integrantes_familia IS NULL OR numero_integrantes_familia >= 1");
            tabla.HasCheckConstraint(
                "ck_postulante_perfil_completo_campos_requeridos",
                """
                estado_perfil <> 'COMPLETO' OR (
                    estado_civil IS NOT NULL
                    AND promedio_actual IS NOT NULL
                    AND trabaja IS NOT NULL
                    AND ocupacion IS NOT NULL
                    AND ingreso_mensual_postulante IS NOT NULL
                    AND numero_integrantes_familia IS NOT NULL
                    AND ingreso_familiar_mensual_total IS NOT NULL
                )
                """);
        });

        builder.HasKey(postulante => postulante.Id)
            .HasName("pk_postulantes");

        builder.Ignore(postulante => postulante.EventosDominio);
        builder.Ignore(postulante => postulante.NombreCompleto);

        ConfiguracionPersistencia.ConfigurarEntidadAuditable(builder);

        builder.Property(postulante => postulante.PersonaId)
            .HasColumnName("persona_id")
            .HasColumnType("uuid");

        builder.Property(postulante => postulante.UsuarioId)
            .HasColumnName("usuario_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(postulante => postulante.NumeroDocumentoIdentidad)
            .HasColumnName("numero_documento_identidad")
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(postulante => postulante.Nombres)
            .HasColumnName("nombres")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(postulante => postulante.Apellidos)
            .HasColumnName("apellidos")
            .HasMaxLength(100)
            .IsRequired();

        builder.OwnsOne(postulante => postulante.DatosContacto, contacto =>
        {
            contacto.Property(valor => valor.CorreoElectronico)
                .HasColumnName("correo_electronico")
                .HasMaxLength(254)
                .IsRequired();

            contacto.Property(valor => valor.Telefono)
                .HasColumnName("telefono")
                .HasMaxLength(20)
                .IsRequired();
        });

        builder.Navigation(postulante => postulante.DatosContacto)
            .IsRequired();

        builder.Property(postulante => postulante.UniversidadAliadaId)
            .HasColumnName("universidad_aliada_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(postulante => postulante.CarreraId)
            .HasColumnName("carrera_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(postulante => postulante.PeriodoActualId)
            .HasColumnName("periodo_actual_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(postulante => postulante.CondicionAcademica)
            .HasColumnName("condicion_academica")
            .ComoCodigoEnum()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(postulante => postulante.AceptaTratamientoDatos)
            .HasColumnName("acepta_tratamiento_datos")
            .IsRequired();

        builder.Property(postulante => postulante.EstadoPerfil)
            .HasColumnName("estado_perfil")
            .ComoCodigoEnum()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(postulante => postulante.FechaRegistro)
            .HasColumnName("fecha_registro")
            .HasColumnType("timestamptz")
            .IsRequired();

        builder.Property(postulante => postulante.EstadoCivil)
            .HasColumnName("estado_civil")
            .ComoCodigoEnumOpcional()
            .HasMaxLength(20);

        builder.Property(postulante => postulante.PromedioActual)
            .HasColumnName("promedio_actual")
            .HasPrecision(5, 2);

        builder.Property(postulante => postulante.Trabaja)
            .HasColumnName("trabaja");

        builder.Property(postulante => postulante.Ocupacion)
            .HasColumnName("ocupacion")
            .ComoCodigoEnumOpcional()
            .HasMaxLength(30);

        builder.Property(postulante => postulante.IngresoMensualPostulante)
            .HasColumnName("ingreso_mensual_postulante")
            .HasPrecision(18, 2);

        builder.Property(postulante => postulante.NumeroIntegrantesFamilia)
            .HasColumnName("numero_integrantes_familia");

        builder.Property(postulante => postulante.IngresoFamiliarMensualTotal)
            .HasColumnName("ingreso_familiar_mensual_total")
            .HasPrecision(18, 2);

        builder.HasMany(postulante => postulante.MiembrosFamiliares)
            .WithOne()
            .HasForeignKey(miembro => miembro.PostulanteId)
            .HasConstraintName("fk_miembros_familiares_postulantes")
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(postulante => postulante.MiembrosFamiliares)
            .HasField("_miembrosFamiliares")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(postulante => postulante.NumeroDocumentoIdentidad)
            .IsUnique()
            .HasDatabaseName("ux_postulante_numero_documento_identidad");

        builder.HasIndex(postulante => postulante.UsuarioId)
            .IsUnique()
            .HasDatabaseName("ux_postulante_usuario_id");
    }
}
