using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ascenso.Postulaciones.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class ConfirmacionRankingConvocatoria : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "clasificacion",
            schema: "postulaciones",
            table: "resultados_consolidados_ranking",
            type: "varchar",
            maxLength: 20,
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "confirmado_por_usuario_id",
            schema: "postulaciones",
            table: "resultados_consolidados_ranking",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "fecha_confirmacion_ranking",
            schema: "postulaciones",
            table: "resultados_consolidados_ranking",
            type: "timestamptz",
            nullable: true);

        migrationBuilder.AddColumn<decimal>(
            name: "monto_estimado_beca",
            schema: "postulaciones",
            table: "resultados_consolidados_ranking",
            type: "numeric(12,2)",
            precision: 12,
            scale: 2,
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "posicion_ranking",
            schema: "postulaciones",
            table: "resultados_consolidados_ranking",
            type: "integer",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "ux_resultados_consolidados_ranking_convocatoria_posicion",
            schema: "postulaciones",
            table: "resultados_consolidados_ranking",
            columns: ["convocatoria_id", "posicion_ranking"],
            unique: true,
            filter: "posicion_ranking IS NOT NULL");

        migrationBuilder.AddCheckConstraint(
            name: "ck_resultados_consolidados_clasificacion_valida",
            schema: "postulaciones",
            table: "resultados_consolidados_ranking",
            sql: "clasificacion IS NULL OR clasificacion IN ('DENTRO_DE_CUPO', 'LISTA_DE_ESPERA')");

        migrationBuilder.AddCheckConstraint(
            name: "ck_resultados_consolidados_confirmacion_coherente",
            schema: "postulaciones",
            table: "resultados_consolidados_ranking",
            sql: "(estado_confirmacion_ranking = 'PROVISIONAL' AND posicion_ranking IS NULL AND monto_estimado_beca IS NULL AND clasificacion IS NULL AND fecha_confirmacion_ranking IS NULL AND confirmado_por_usuario_id IS NULL) OR (estado_confirmacion_ranking = 'CONFIRMADO' AND posicion_ranking IS NOT NULL AND monto_estimado_beca IS NOT NULL AND clasificacion IS NOT NULL AND fecha_confirmacion_ranking IS NOT NULL AND confirmado_por_usuario_id IS NOT NULL) OR estado_confirmacion_ranking NOT IN ('PROVISIONAL', 'CONFIRMADO')");

        migrationBuilder.AddCheckConstraint(
            name: "ck_resultados_consolidados_ranking_campos_rango",
            schema: "postulaciones",
            table: "resultados_consolidados_ranking",
            sql: "(posicion_ranking IS NULL OR posicion_ranking >= 1) AND (monto_estimado_beca IS NULL OR monto_estimado_beca > 0)");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "ux_resultados_consolidados_ranking_convocatoria_posicion",
            schema: "postulaciones",
            table: "resultados_consolidados_ranking");

        migrationBuilder.DropCheckConstraint(
            name: "ck_resultados_consolidados_clasificacion_valida",
            schema: "postulaciones",
            table: "resultados_consolidados_ranking");

        migrationBuilder.DropCheckConstraint(
            name: "ck_resultados_consolidados_confirmacion_coherente",
            schema: "postulaciones",
            table: "resultados_consolidados_ranking");

        migrationBuilder.DropCheckConstraint(
            name: "ck_resultados_consolidados_ranking_campos_rango",
            schema: "postulaciones",
            table: "resultados_consolidados_ranking");

        migrationBuilder.DropColumn(
            name: "clasificacion",
            schema: "postulaciones",
            table: "resultados_consolidados_ranking");

        migrationBuilder.DropColumn(
            name: "confirmado_por_usuario_id",
            schema: "postulaciones",
            table: "resultados_consolidados_ranking");

        migrationBuilder.DropColumn(
            name: "fecha_confirmacion_ranking",
            schema: "postulaciones",
            table: "resultados_consolidados_ranking");

        migrationBuilder.DropColumn(
            name: "monto_estimado_beca",
            schema: "postulaciones",
            table: "resultados_consolidados_ranking");

        migrationBuilder.DropColumn(
            name: "posicion_ranking",
            schema: "postulaciones",
            table: "resultados_consolidados_ranking");
    }
}
