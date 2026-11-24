using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ascenso.Postulaciones.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class AsignacionesEvaluacion : Migration
{
    private static readonly string[] _columnasIndiceComiteEstado = ["comite_evaluador_id", "estado"];
    private static readonly string[] _columnasIndiceEvaluadorEstado = ["usuario_evaluador_id", "estado", "fecha_limite_evaluacion"];
    private static readonly string[] _columnasIndicePostulacionEvaluadorVigente = ["postulacion_id", "usuario_evaluador_id"];

    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "asignaciones_evaluacion",
            schema: "postulaciones",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                comite_evaluador_id = table.Column<Guid>(type: "uuid", nullable: false),
                convocatoria_id = table.Column<Guid>(type: "uuid", nullable: false),
                postulacion_id = table.Column<Guid>(type: "uuid", nullable: false),
                miembro_comite_id = table.Column<Guid>(type: "uuid", nullable: false),
                usuario_evaluador_id = table.Column<Guid>(type: "uuid", nullable: false),
                fecha_asignacion = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                fecha_limite_evaluacion = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                estado = table.Column<string>(type: "varchar", maxLength: 30, nullable: false),
                fecha_creacion = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                usuario_creacion_id = table.Column<Guid>(type: "uuid", nullable: false),
                fecha_ultima_modificacion = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                usuario_modificacion_id = table.Column<Guid>(type: "uuid", nullable: false),
                version = table.Column<int>(type: "integer", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_asignaciones_evaluacion", x => x.id);
                table.CheckConstraint("ck_asignaciones_estado_valido", "estado IN ('PENDIENTE', 'EN_PROGRESO', 'COMPLETADA', 'REASIGNADA_POR_CONFLICTO')");
                table.CheckConstraint("ck_asignaciones_fecha_limite_posterior", "fecha_limite_evaluacion > fecha_asignacion");
                table.ForeignKey(
                    name: "fk_asignaciones_comites",
                    column: x => x.comite_evaluador_id,
                    principalSchema: "postulaciones",
                    principalTable: "comites_evaluadores",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "fk_asignaciones_miembros_comite",
                    column: x => x.miembro_comite_id,
                    principalSchema: "postulaciones",
                    principalTable: "miembros_comite",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "ix_asignaciones_comite_estado",
            schema: "postulaciones",
            table: "asignaciones_evaluacion",
            columns: _columnasIndiceComiteEstado);

        migrationBuilder.CreateIndex(
            name: "IX_asignaciones_evaluacion_miembro_comite_id",
            schema: "postulaciones",
            table: "asignaciones_evaluacion",
            column: "miembro_comite_id");

        migrationBuilder.CreateIndex(
            name: "ix_asignaciones_evaluador_estado",
            schema: "postulaciones",
            table: "asignaciones_evaluacion",
            columns: _columnasIndiceEvaluadorEstado);

        migrationBuilder.CreateIndex(
            name: "ix_asignaciones_postulacion",
            schema: "postulaciones",
            table: "asignaciones_evaluacion",
            column: "postulacion_id");

        migrationBuilder.CreateIndex(
            name: "ux_asignaciones_postulacion_evaluador_vigente",
            schema: "postulaciones",
            table: "asignaciones_evaluacion",
            columns: _columnasIndicePostulacionEvaluadorVigente,
            unique: true,
            filter: "estado <> 'REASIGNADA_POR_CONFLICTO'");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "asignaciones_evaluacion",
            schema: "postulaciones");
    }
}
