using System.Text;
using Ascenso.Becas.Domain.Desembolsos.Agregados;
using Ascenso.Becas.Domain.Desembolsos.Entidades;
using Ascenso.Becas.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ascenso.Becas.Infrastructure.Persistence.Configuraciones;

internal static class ColumnasDesembolso
{
    public static void Aplicar<TEntity>(EntityTypeBuilder<TEntity> builder) where TEntity : class
    {
        foreach (var propiedad in builder.Metadata.GetProperties())
        {
            var nombre = propiedad.Name;
            var columna = new StringBuilder(nombre.Length + 4);
            for (var indice = 0; indice < nombre.Length; indice++)
            {
                if (indice > 0 && char.IsUpper(nombre[indice])) columna.Append('_');
                columna.Append(char.ToLowerInvariant(nombre[indice]));
            }
            propiedad.SetColumnName(columna.ToString());
        }
    }
}
internal sealed class CalendarioDesembolsoConfiguracion : IEntityTypeConfiguration<CalendarioDesembolso>
{
    public void Configure(EntityTypeBuilder<CalendarioDesembolso> builder)
    {
        builder.ToTable("calendario_desembolso", tabla =>
        {
            tabla.HasCheckConstraint("ck_calendario_montos", "monto_maximo_aprobado_periodo >= 0 AND suma_cuotas_programadas >= 0 AND suma_cuotas_programadas <= monto_maximo_aprobado_periodo");
            tabla.HasCheckConstraint("ck_calendario_fechas", "fecha_fin >= fecha_inicio AND numero_cuotas > 0");
            tabla.HasCheckConstraint("ck_calendario_estado", $"estado_plan IN ({ConfiguracionPersistencia.ValoresSql<EstadoPlanDesembolso>()})");
            tabla.HasCheckConstraint("ck_calendario_frecuencia", $"frecuencia IN ({ConfiguracionPersistencia.ValoresSql<FrecuenciaDesembolso>()})");
            tabla.HasCheckConstraint("ck_calendario_destinatario", $"destinatario_predeterminado IN ({ConfiguracionPersistencia.ValoresSql<DestinatarioPago>()})");
        });
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Id).ValueGeneratedNever();
        builder.Ignore(item => item.EventosDominio);
        builder.Ignore(item => item.Desembolsos);
        builder.HasMany<Desembolso>("_desembolsos").WithOne().HasForeignKey(item => item.CalendarioDesembolsoId);
        builder.Navigation("_desembolsos").UsePropertyAccessMode(PropertyAccessMode.Field);
        ConfiguracionPersistencia.ConfigurarAuditoria(builder);
        builder.Property(item => item.Frecuencia).ComoCodigoEnum();
        builder.Property(item => item.DestinatarioPredeterminado).ComoCodigoEnum();
        builder.Property(item => item.EstadoPlan).ComoCodigoEnum();
        builder.Property(item => item.MontoMaximoAprobadoPeriodo).HasPrecision(18, 2);
        builder.Property(item => item.SumaCuotasProgramadas).HasPrecision(18, 2);
        builder.HasIndex(item => new { item.BecaId, item.PeriodoAcademicoId }).IsUnique();
        ColumnasDesembolso.Aplicar(builder);
    }
}

internal sealed class DesembolsoConfiguracion : IEntityTypeConfiguration<Desembolso>
{
    public void Configure(EntityTypeBuilder<Desembolso> builder)
    {
        builder.ToTable("desembolso", tabla =>
        {
            tabla.HasCheckConstraint("ck_desembolso_estado", $"estado IN ({ConfiguracionPersistencia.ValoresSql<EstadoDesembolso>()})");
            tabla.HasCheckConstraint("ck_desembolso_montos", "monto_programado > 0 AND (monto_ejecutado IS NULL OR (monto_ejecutado > 0 AND monto_ejecutado <= monto_programado))");
            tabla.HasCheckConstraint("ck_desembolso_intentos", "intentos_ejecucion >= 0");
            tabla.HasCheckConstraint("ck_desembolso_destinatario", $"destinatario IN ({ConfiguracionPersistencia.ValoresSql<DestinatarioPago>()})");
            tabla.HasCheckConstraint("ck_desembolso_metodo", $"metodo_pago IS NULL OR metodo_pago IN ({ConfiguracionPersistencia.ValoresSql<MetodoPago>()})");
        });
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Id).ValueGeneratedNever();
        builder.Ignore(item => item.Incidencias);
        builder.HasMany<IncidenciaDesembolso>("_incidencias").WithOne().HasForeignKey(item => item.DesembolsoId);
        builder.Navigation("_incidencias").UsePropertyAccessMode(PropertyAccessMode.Field);
        ConfiguracionPersistencia.ConfigurarAuditoria(builder);
        builder.Property(item => item.Destinatario).ComoCodigoEnum();
        builder.Property(item => item.MetodoPago).ComoCodigoEnumOpcional();
        builder.Property(item => item.Estado).ComoCodigoEnum();
        builder.Property(item => item.MontoProgramado).HasPrecision(18, 2);
        builder.Property(item => item.MontoEjecutado).HasPrecision(18, 2);
        builder.HasIndex(item => new { item.CalendarioDesembolsoId, item.NumeroCuota, item.IntentosEjecucion }).IsUnique();
        builder.HasIndex(item => new { item.CalendarioDesembolsoId, item.NumeroCuota }).IsUnique().HasFilter("es_vigente = true");
        ColumnasDesembolso.Aplicar(builder);
    }
}

internal sealed class DatosPagoBecarioConfiguracion : IEntityTypeConfiguration<DatosPagoBecario>
{
    public void Configure(EntityTypeBuilder<DatosPagoBecario> builder)
    {
        builder.ToTable("datos_pago_becario", tabla =>
        {
            tabla.HasCheckConstraint("ck_datos_pago_estado", $"estado IN ({ConfiguracionPersistencia.ValoresSql<EstadoDatosPago>()})");
            tabla.HasCheckConstraint("ck_datos_pago_tipo_cuenta", $"tipo_cuenta IN ({ConfiguracionPersistencia.ValoresSql<TipoCuentaBancaria>()})");
            tabla.HasCheckConstraint("ck_datos_pago_cifrado", "numero_cuenta_cifrado_aes_gcm LIKE 'v1:%'");
        });
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Id).ValueGeneratedNever();
        builder.Ignore(item => item.EventosDominio);
        ConfiguracionPersistencia.ConfigurarAuditoria(builder);
        builder.Property(item => item.TipoCuenta).ComoCodigoEnum();
        builder.Property(item => item.Estado).ComoCodigoEnum();
        builder.HasIndex(item => item.BecaId).IsUnique().HasFilter("estado = 'ACTIVO'");
        ColumnasDesembolso.Aplicar(builder);
    }
}

internal sealed class IncidenciaDesembolsoConfiguracion : IEntityTypeConfiguration<IncidenciaDesembolso>
{
    public void Configure(EntityTypeBuilder<IncidenciaDesembolso> builder)
    {
        builder.ToTable("incidencia_desembolso", tabla =>
        {
            tabla.HasCheckConstraint("ck_incidencia_estado", $"estado IN ({ConfiguracionPersistencia.ValoresSql<EstadoIncidencia>()})");
            tabla.HasCheckConstraint("ck_incidencia_tipo", $"tipo IN ({ConfiguracionPersistencia.ValoresSql<TipoIncidenciaDesembolso>()})");
            tabla.HasCheckConstraint("ck_incidencia_observacion", "length(trim(observacion_banco)) > 0");
        });
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Id).ValueGeneratedNever();
        ConfiguracionPersistencia.ConfigurarAuditoria(builder);
        builder.Property(item => item.Tipo).ComoCodigoEnum();
        builder.Property(item => item.Estado).ComoCodigoEnum();
        ColumnasDesembolso.Aplicar(builder);
    }
}
