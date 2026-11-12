using Ascenso.Becas.Infrastructure.Persistence.Modelos;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ascenso.Becas.Infrastructure.Persistence.Configuraciones;

internal sealed class IdempotencyRequestConfiguracion : IEntityTypeConfiguration<IdempotencyRequest>
{
    public void Configure(EntityTypeBuilder<IdempotencyRequest> builder)
    {
        builder.ToTable("idempotency_request", tabla =>
        {
            tabla.HasCheckConstraint(
                "ck_idempotency_request_status_code_valido",
                "status_code IS NULL OR (status_code >= 100 AND status_code <= 599)");
            tabla.HasCheckConstraint(
                "ck_idempotency_request_expiracion_valida",
                "expira_en > creado_en");
        });

        builder.HasKey(solicitud => solicitud.Id).HasName("pk_idempotency_request");

        builder.Property(solicitud => solicitud.Id)
            .HasColumnName("id")
            .HasColumnType("uuid");

        builder.Property(solicitud => solicitud.Endpoint)
            .HasColumnName("endpoint")
            .HasMaxLength(200);

        builder.Property(solicitud => solicitud.Clave)
            .HasColumnName("clave")
            .HasMaxLength(128);

        builder.Property(solicitud => solicitud.HashPayload)
            .HasColumnName("hash_payload")
            .HasMaxLength(64)
            .IsFixedLength();

        builder.Property(solicitud => solicitud.Estado)
            .HasColumnName("estado")
            .HasMaxLength(30);

        builder.Property(solicitud => solicitud.StatusCode)
            .HasColumnName("status_code");

        builder.Property(solicitud => solicitud.ResponseBody)
            .HasColumnName("response_body")
            .HasColumnType("text");

        builder.Property(solicitud => solicitud.RecursoId)
            .HasColumnName("recurso_id")
            .HasColumnType("uuid");

        builder.Property(solicitud => solicitud.CreadoEn)
            .HasColumnName("creado_en")
            .HasColumnType("timestamptz");

        builder.Property(solicitud => solicitud.ExpiraEn)
            .HasColumnName("expira_en")
            .HasColumnType("timestamptz");

        builder.HasIndex(solicitud => new { solicitud.Endpoint, solicitud.Clave })
            .IsUnique()
            .HasDatabaseName("ux_idempotency_request_endpoint_clave");
    }
}
