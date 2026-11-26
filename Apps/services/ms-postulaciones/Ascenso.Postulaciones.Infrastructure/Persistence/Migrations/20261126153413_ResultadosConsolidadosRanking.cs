using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ascenso.Postulaciones.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class ResultadosConsolidadosRanking : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "resultados_consolidados_ranking",
            schema: "postulaciones",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                convocatoria_id = table.Column<Guid>(type: "uuid", nullable: false),
                postulacion_id = table.Column<Guid>(type: "uuid", nullable: false),
                excepcion_evaluador_unico = table.Column<bool>(type: "boolean", nullable: false),
                justificacion_evaluador_unico = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                autorizador_excepcion_id = table.Column<Guid>(type: "uuid", nullable: true),
                cantidad_evaluaciones_validas = table.Column<int>(type: "integer", nullable: false),
                puntaje_consolidado = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                diferencia_entre_evaluadores = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                umbral_discrepancia_aplicado = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                alerta_discrepancia = table.Column<bool>(type: "boolean", nullable: false),
                discrepancia_revisada = table.Column<bool>(type: "boolean", nullable: false),
                comentario_revision_discrepancia = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                revisado_por_usuario_id = table.Column<Guid>(type: "uuid", nullable: true),
                fecha_revision_discrepancia = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                estado_confirmacion_ranking = table.Column<string>(type: "varchar", maxLength: 20, nullable: false),
                fecha_consolidacion = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                fecha_creacion = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                usuario_creacion_id = table.Column<Guid>(type: "uuid", nullable: false),
                fecha_ultima_modificacion = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                usuario_modificacion_id = table.Column<Guid>(type: "uuid", nullable: false),
                version = table.Column<int>(type: "integer", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_resultados_consolidados_ranking", x => x.id);
                table.CheckConstraint("ck_resultados_consolidados_alerta_coherente", "(excepcion_evaluador_unico = true AND alerta_discrepancia = false) OR (excepcion_evaluador_unico = false AND alerta_discrepancia = (diferencia_entre_evaluadores > umbral_discrepancia_aplicado))");
                table.CheckConstraint("ck_resultados_consolidados_diferencia_rango", "diferencia_entre_evaluadores >= 0 AND diferencia_entre_evaluadores <= 100");
                table.CheckConstraint("ck_resultados_consolidados_estado_valido", "estado_confirmacion_ranking IN ('PROVISIONAL', 'CONFIRMADO')");
                table.CheckConstraint("ck_resultados_consolidados_excepcion_coherente", "(excepcion_evaluador_unico = true AND cantidad_evaluaciones_validas = 1 AND justificacion_evaluador_unico IS NOT NULL AND length(btrim(justificacion_evaluador_unico)) > 0 AND autorizador_excepcion_id IS NOT NULL AND diferencia_entre_evaluadores = 0 AND alerta_discrepancia = false) OR (excepcion_evaluador_unico = false AND cantidad_evaluaciones_validas >= 2 AND justificacion_evaluador_unico IS NULL AND autorizador_excepcion_id IS NULL)");
                table.CheckConstraint("ck_resultados_consolidados_puntaje_rango", "puntaje_consolidado >= 0 AND puntaje_consolidado <= 100");
                table.CheckConstraint("ck_resultados_consolidados_revision_coherente", "(discrepancia_revisada = false AND comentario_revision_discrepancia IS NULL AND revisado_por_usuario_id IS NULL AND fecha_revision_discrepancia IS NULL) OR (discrepancia_revisada = true AND alerta_discrepancia = true AND comentario_revision_discrepancia IS NOT NULL AND length(btrim(comentario_revision_discrepancia)) > 0 AND revisado_por_usuario_id IS NOT NULL AND fecha_revision_discrepancia IS NOT NULL)");
                table.CheckConstraint("ck_resultados_consolidados_umbral_rango", "umbral_discrepancia_aplicado > 0 AND umbral_discrepancia_aplicado <= 100");
            });

        migrationBuilder.CreateIndex(
            name: "ix_resultados_consolidados_ranking_convocatoria_discrepancia",
            schema: "postulaciones",
            table: "resultados_consolidados_ranking",
            columns: ["convocatoria_id", "alerta_discrepancia", "discrepancia_revisada"]);

        migrationBuilder.CreateIndex(
            name: "ix_resultados_consolidados_ranking_convocatoria_puntaje",
            schema: "postulaciones",
            table: "resultados_consolidados_ranking",
            columns: ["convocatoria_id", "puntaje_consolidado", "fecha_consolidacion", "id"]);

        migrationBuilder.CreateIndex(
            name: "ux_resultados_consolidados_ranking_postulacion",
            schema: "postulaciones",
            table: "resultados_consolidados_ranking",
            column: "postulacion_id",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "resultados_consolidados_ranking",
            schema: "postulaciones");
    }
}
