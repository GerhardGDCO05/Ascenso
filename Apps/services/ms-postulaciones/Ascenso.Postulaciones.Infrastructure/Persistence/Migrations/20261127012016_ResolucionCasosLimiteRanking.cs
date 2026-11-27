using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ascenso.Postulaciones.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class ResolucionCasosLimiteRanking : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(
            name: "decidido_por_usuario_id",
            schema: "postulaciones",
            table: "resultados_consolidados_ranking",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "fecha_decision_caso_limite",
            schema: "postulaciones",
            table: "resultados_consolidados_ranking",
            type: "timestamptz",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "justificacion_desempate_caso_limite",
            schema: "postulaciones",
            table: "resultados_consolidados_ranking",
            type: "character varying(1000)",
            maxLength: 1000,
            nullable: true);

        migrationBuilder.AddColumn<bool>(
            name: "seleccionado_en_caso_limite",
            schema: "postulaciones",
            table: "resultados_consolidados_ranking",
            type: "boolean",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "tipo_caso_limite",
            schema: "postulaciones",
            table: "resultados_consolidados_ranking",
            type: "varchar",
            maxLength: 40,
            nullable: true);

        migrationBuilder.AddCheckConstraint(
            name: "ck_resultados_consolidados_caso_limite_coherente",
            schema: "postulaciones",
            table: "resultados_consolidados_ranking",
            sql: "(tipo_caso_limite IS NULL AND seleccionado_en_caso_limite IS NULL AND justificacion_desempate_caso_limite IS NULL AND decidido_por_usuario_id IS NULL AND fecha_decision_caso_limite IS NULL) OR (tipo_caso_limite IS NOT NULL AND seleccionado_en_caso_limite IS NOT NULL AND justificacion_desempate_caso_limite IS NOT NULL AND length(btrim(justificacion_desempate_caso_limite)) >= 10 AND decidido_por_usuario_id IS NOT NULL AND fecha_decision_caso_limite IS NOT NULL) OR (tipo_caso_limite IS NOT NULL AND tipo_caso_limite NOT IN ('EMPATE_POR_CUPO', 'EMPATE_POR_PRESUPUESTO', 'CASO_LIMITE_PRESUPUESTARIO'))");

        migrationBuilder.AddCheckConstraint(
            name: "ck_resultados_consolidados_tipo_caso_limite_valido",
            schema: "postulaciones",
            table: "resultados_consolidados_ranking",
            sql: "tipo_caso_limite IS NULL OR tipo_caso_limite IN ('EMPATE_POR_CUPO', 'EMPATE_POR_PRESUPUESTO', 'CASO_LIMITE_PRESUPUESTARIO')");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropCheckConstraint(
            name: "ck_resultados_consolidados_caso_limite_coherente",
            schema: "postulaciones",
            table: "resultados_consolidados_ranking");

        migrationBuilder.DropCheckConstraint(
            name: "ck_resultados_consolidados_tipo_caso_limite_valido",
            schema: "postulaciones",
            table: "resultados_consolidados_ranking");

        migrationBuilder.DropColumn(
            name: "decidido_por_usuario_id",
            schema: "postulaciones",
            table: "resultados_consolidados_ranking");

        migrationBuilder.DropColumn(
            name: "fecha_decision_caso_limite",
            schema: "postulaciones",
            table: "resultados_consolidados_ranking");

        migrationBuilder.DropColumn(
            name: "justificacion_desempate_caso_limite",
            schema: "postulaciones",
            table: "resultados_consolidados_ranking");

        migrationBuilder.DropColumn(
            name: "seleccionado_en_caso_limite",
            schema: "postulaciones",
            table: "resultados_consolidados_ranking");

        migrationBuilder.DropColumn(
            name: "tipo_caso_limite",
            schema: "postulaciones",
            table: "resultados_consolidados_ranking");
    }
}
