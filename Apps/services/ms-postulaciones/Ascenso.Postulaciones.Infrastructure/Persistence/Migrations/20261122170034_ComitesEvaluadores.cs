using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ascenso.Postulaciones.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class ComitesEvaluadores : Migration
{
    private static readonly string[] _columnasIndiceComitesProgramaConvocatoria = ["programa_beca_id", "convocatoria_id"];
    private static readonly string[] _columnasIndiceMiembrosComiteUsuarioActivo = ["comite_evaluador_id", "usuario_evaluador_id"];

    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "comites_evaluadores",
            schema: "postulaciones",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                programa_beca_id = table.Column<Guid>(type: "uuid", nullable: false),
                convocatoria_id = table.Column<Guid>(type: "uuid", nullable: false),
                nombre = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                jefatura_usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                fecha_conformacion = table.Column<DateOnly>(type: "date", nullable: false),
                conformado_por_usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                activo = table.Column<bool>(type: "boolean", nullable: false),
                fecha_creacion = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                usuario_creacion_id = table.Column<Guid>(type: "uuid", nullable: false),
                fecha_ultima_modificacion = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                usuario_modificacion_id = table.Column<Guid>(type: "uuid", nullable: false),
                version = table.Column<int>(type: "integer", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_comites_evaluadores", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "miembros_comite",
            schema: "postulaciones",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                comite_evaluador_id = table.Column<Guid>(type: "uuid", nullable: false),
                usuario_evaluador_id = table.Column<Guid>(type: "uuid", nullable: false),
                tipo = table.Column<string>(type: "varchar", maxLength: 10, nullable: false),
                rol = table.Column<string>(type: "varchar", maxLength: 15, nullable: false),
                fecha_asignacion = table.Column<DateOnly>(type: "date", nullable: false),
                fecha_desvinculacion = table.Column<DateOnly>(type: "date", nullable: true),
                activo = table.Column<bool>(type: "boolean", nullable: false),
                fecha_creacion = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                usuario_creacion_id = table.Column<Guid>(type: "uuid", nullable: false),
                fecha_ultima_modificacion = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                usuario_modificacion_id = table.Column<Guid>(type: "uuid", nullable: false),
                version = table.Column<int>(type: "integer", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_miembros_comite", x => x.id);
                table.CheckConstraint("ck_miembros_comite_desvinculacion_coherente", "(activo AND fecha_desvinculacion IS NULL) OR (NOT activo AND fecha_desvinculacion IS NOT NULL AND fecha_desvinculacion >= fecha_asignacion)");
                table.CheckConstraint("ck_miembros_comite_jefatura_interna", "rol <> 'JEFATURA' OR tipo = 'INTERNO'");
                table.CheckConstraint("ck_miembros_comite_rol_valido", "rol IN ('JEFATURA', 'EVALUADOR')");
                table.CheckConstraint("ck_miembros_comite_tipo_valido", "tipo IN ('INTERNO', 'EXTERNO')");
                table.ForeignKey(
                    name: "fk_miembros_comite_comites",
                    column: x => x.comite_evaluador_id,
                    principalSchema: "postulaciones",
                    principalTable: "comites_evaluadores",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "ux_comites_programa_convocatoria",
            schema: "postulaciones",
            table: "comites_evaluadores",
            columns: _columnasIndiceComitesProgramaConvocatoria,
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ux_miembros_comite_jefatura_activa",
            schema: "postulaciones",
            table: "miembros_comite",
            column: "comite_evaluador_id",
            unique: true,
            filter: "activo AND rol = 'JEFATURA'");

        migrationBuilder.CreateIndex(
            name: "ux_miembros_comite_usuario_activo",
            schema: "postulaciones",
            table: "miembros_comite",
            columns: _columnasIndiceMiembrosComiteUsuarioActivo,
            unique: true,
            filter: "activo");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "miembros_comite",
            schema: "postulaciones");

        migrationBuilder.DropTable(
            name: "comites_evaluadores",
            schema: "postulaciones");
    }
}
