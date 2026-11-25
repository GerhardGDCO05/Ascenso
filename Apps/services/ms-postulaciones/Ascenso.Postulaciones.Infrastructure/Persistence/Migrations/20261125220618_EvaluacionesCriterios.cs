using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ascenso.Postulaciones.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class EvaluacionesCriterios : Migration
{
    private static readonly string[] _columnasIndiceEvaluacionCriterio = ["evaluacion_id", "criterio_evaluacion_id"];
    private static readonly string[] _columnasIndiceEvaluadorEstado = ["evaluador_id", "estado", "fecha_ultima_modificacion"];
    private static readonly string[] _columnasIndicePostulacionEstado = ["postulacion_id", "estado"];

    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "evaluaciones",
            schema: "postulaciones",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                asignacion_evaluacion_id = table.Column<Guid>(type: "uuid", nullable: false),
                postulacion_id = table.Column<Guid>(type: "uuid", nullable: false),
                convocatoria_id = table.Column<Guid>(type: "uuid", nullable: false),
                rubrica_evaluacion_id = table.Column<Guid>(type: "uuid", nullable: false),
                evaluador_id = table.Column<Guid>(type: "uuid", nullable: false),
                fecha_inicio = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                fecha_envio = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                puntaje_total_ponderado = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                comentarios_generales = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                estado = table.Column<string>(type: "varchar", maxLength: 30, nullable: false),
                motivo_invalidacion = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                fecha_creacion = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                usuario_creacion_id = table.Column<Guid>(type: "uuid", nullable: false),
                fecha_ultima_modificacion = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                usuario_modificacion_id = table.Column<Guid>(type: "uuid", nullable: false),
                version = table.Column<int>(type: "integer", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_evaluaciones", x => x.id);
                table.CheckConstraint("ck_evaluaciones_comentarios_generales_no_vacios", "comentarios_generales IS NULL OR length(btrim(comentarios_generales)) > 0");
                table.CheckConstraint("ck_evaluaciones_envio_coherente", "(estado = 'BORRADOR' AND fecha_envio IS NULL AND puntaje_total_ponderado IS NULL AND motivo_invalidacion IS NULL) OR (estado = 'ENVIADA_DEFINITIVA' AND fecha_envio IS NOT NULL AND puntaje_total_ponderado IS NOT NULL AND motivo_invalidacion IS NULL) OR (estado = 'INVALIDADA_POR_CONFLICTO' AND motivo_invalidacion IS NOT NULL AND length(btrim(motivo_invalidacion)) > 0)");
                table.CheckConstraint("ck_evaluaciones_estado_valido", "estado IN ('BORRADOR', 'ENVIADA_DEFINITIVA', 'INVALIDADA_POR_CONFLICTO')");
                table.CheckConstraint("ck_evaluaciones_puntaje_total_rango", "puntaje_total_ponderado IS NULL OR (puntaje_total_ponderado >= 0 AND puntaje_total_ponderado <= 100)");
                table.ForeignKey(
                    name: "fk_evaluaciones_asignaciones_evaluacion",
                    column: x => x.asignacion_evaluacion_id,
                    principalSchema: "postulaciones",
                    principalTable: "asignaciones_evaluacion",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "fk_evaluaciones_rubricas",
                    column: x => x.rubrica_evaluacion_id,
                    principalSchema: "postulaciones",
                    principalTable: "rubricas",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "calificaciones_criterio",
            schema: "postulaciones",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                evaluacion_id = table.Column<Guid>(type: "uuid", nullable: false),
                criterio_evaluacion_id = table.Column<Guid>(type: "uuid", nullable: false),
                puntaje_asignado = table.Column<decimal>(type: "numeric(7,2)", precision: 7, scale: 2, nullable: false),
                comentario = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                fecha_creacion = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                usuario_creacion_id = table.Column<Guid>(type: "uuid", nullable: false),
                fecha_ultima_modificacion = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                usuario_modificacion_id = table.Column<Guid>(type: "uuid", nullable: false),
                version = table.Column<int>(type: "integer", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_calificaciones_criterio", x => x.id);
                table.CheckConstraint("ck_calificaciones_criterio_comentario_no_vacio", "comentario IS NULL OR length(btrim(comentario)) > 0");
                table.CheckConstraint("ck_calificaciones_criterio_puntaje_rango", "puntaje_asignado >= 0 AND puntaje_asignado <= 1000");
                table.ForeignKey(
                    name: "fk_calificaciones_criterio_criterios_evaluacion",
                    column: x => x.criterio_evaluacion_id,
                    principalSchema: "postulaciones",
                    principalTable: "criterios_evaluacion",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "fk_calificaciones_criterio_evaluaciones",
                    column: x => x.evaluacion_id,
                    principalSchema: "postulaciones",
                    principalTable: "evaluaciones",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_calificaciones_criterio_criterio_evaluacion_id",
            schema: "postulaciones",
            table: "calificaciones_criterio",
            column: "criterio_evaluacion_id");

        migrationBuilder.CreateIndex(
            name: "ux_calificaciones_criterio_evaluacion_criterio",
            schema: "postulaciones",
            table: "calificaciones_criterio",
            columns: _columnasIndiceEvaluacionCriterio,
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_evaluaciones_evaluador_estado",
            schema: "postulaciones",
            table: "evaluaciones",
            columns: _columnasIndiceEvaluadorEstado);

        migrationBuilder.CreateIndex(
            name: "ix_evaluaciones_postulacion_estado",
            schema: "postulaciones",
            table: "evaluaciones",
            columns: _columnasIndicePostulacionEstado);

        migrationBuilder.CreateIndex(
            name: "IX_evaluaciones_rubrica_evaluacion_id",
            schema: "postulaciones",
            table: "evaluaciones",
            column: "rubrica_evaluacion_id");

        migrationBuilder.CreateIndex(
            name: "ux_evaluaciones_asignacion_evaluacion",
            schema: "postulaciones",
            table: "evaluaciones",
            column: "asignacion_evaluacion_id",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "calificaciones_criterio",
            schema: "postulaciones");

        migrationBuilder.DropTable(
            name: "evaluaciones",
            schema: "postulaciones");
    }
}
