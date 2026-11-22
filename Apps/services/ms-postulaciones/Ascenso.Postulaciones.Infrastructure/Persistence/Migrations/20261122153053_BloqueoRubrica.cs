using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ascenso.Postulaciones.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class BloqueoRubrica : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "fecha_bloqueo",
            schema: "postulaciones",
            table: "rubricas",
            type: "timestamptz",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "motivo_bloqueo",
            schema: "postulaciones",
            table: "rubricas",
            type: "varchar",
            maxLength: 30,
            nullable: true);

        migrationBuilder.AddCheckConstraint(
            name: "ck_rubricas_bloqueo_coherente",
            schema: "postulaciones",
            table: "rubricas",
            sql: "(estado = 'BLOQUEADA' AND motivo_bloqueo IS NOT NULL AND fecha_bloqueo IS NOT NULL) OR (estado IN ('BORRADOR', 'PUBLICADA') AND motivo_bloqueo IS NULL AND fecha_bloqueo IS NULL)");

        migrationBuilder.AddCheckConstraint(
            name: "ck_rubricas_motivo_bloqueo_valido",
            schema: "postulaciones",
            table: "rubricas",
            sql: "motivo_bloqueo IS NULL OR motivo_bloqueo IN ('INICIO_EVALUACION', 'PRIMERA_CALIFICACION')");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropCheckConstraint(
            name: "ck_rubricas_bloqueo_coherente",
            schema: "postulaciones",
            table: "rubricas");

        migrationBuilder.DropCheckConstraint(
            name: "ck_rubricas_motivo_bloqueo_valido",
            schema: "postulaciones",
            table: "rubricas");

        migrationBuilder.DropColumn(
            name: "fecha_bloqueo",
            schema: "postulaciones",
            table: "rubricas");

        migrationBuilder.DropColumn(
            name: "motivo_bloqueo",
            schema: "postulaciones",
            table: "rubricas");
    }
}
