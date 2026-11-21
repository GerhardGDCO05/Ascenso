using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ascenso.Postulaciones.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class RubricasEvaluacion : Migration
{
    private static readonly string[] ColumnasIndiceCriteriosRubricaOrden = ["rubrica_evaluacion_id", "orden"];
    private static readonly string[] ColumnasIndiceRubricasProgramaConvocatoria = ["programa_beca_id", "convocatoria_id"];

    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "rubricas",
            schema: "postulaciones",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                programa_beca_id = table.Column<Guid>(type: "uuid", nullable: false),
                convocatoria_id = table.Column<Guid>(type: "uuid", nullable: false),
                nombre = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                estado = table.Column<string>(type: "varchar", maxLength: 20, nullable: false),
                fecha_publicacion = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                fecha_creacion = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                usuario_creacion_id = table.Column<Guid>(type: "uuid", nullable: false),
                fecha_ultima_modificacion = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                usuario_modificacion_id = table.Column<Guid>(type: "uuid", nullable: false),
                version = table.Column<int>(type: "integer", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_rubricas", x => x.id);
                table.CheckConstraint("ck_rubricas_estado_valido", "estado IN ('BORRADOR', 'PUBLICADA', 'BLOQUEADA')");
            });

        migrationBuilder.CreateTable(
            name: "criterios_evaluacion",
            schema: "postulaciones",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                rubrica_evaluacion_id = table.Column<Guid>(type: "uuid", nullable: false),
                nombre = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                descripcion = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                peso_porcentual = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                puntaje_minimo = table.Column<decimal>(type: "numeric(7,2)", precision: 7, scale: 2, nullable: false),
                puntaje_maximo = table.Column<decimal>(type: "numeric(7,2)", precision: 7, scale: 2, nullable: false),
                orden = table.Column<int>(type: "integer", nullable: false),
                fecha_creacion = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                usuario_creacion_id = table.Column<Guid>(type: "uuid", nullable: false),
                fecha_ultima_modificacion = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                usuario_modificacion_id = table.Column<Guid>(type: "uuid", nullable: false),
                version = table.Column<int>(type: "integer", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_criterios_evaluacion", x => x.id);
                table.CheckConstraint("ck_criterios_evaluacion_orden_positivo", "orden > 0");
                table.CheckConstraint("ck_criterios_evaluacion_peso_rango", "peso_porcentual > 0 AND peso_porcentual <= 100");
                table.CheckConstraint("ck_criterios_evaluacion_puntaje_rango", "puntaje_minimo >= 0 AND puntaje_maximo <= 1000 AND puntaje_minimo < puntaje_maximo");
                table.ForeignKey(
                    name: "fk_criterios_evaluacion_rubricas",
                    column: x => x.rubrica_evaluacion_id,
                    principalSchema: "postulaciones",
                    principalTable: "rubricas",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "ux_criterios_evaluacion_rubrica_orden",
            schema: "postulaciones",
            table: "criterios_evaluacion",
            columns: ColumnasIndiceCriteriosRubricaOrden,
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ux_rubricas_programa_convocatoria",
            schema: "postulaciones",
            table: "rubricas",
            columns: ColumnasIndiceRubricasProgramaConvocatoria,
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "criterios_evaluacion",
            schema: "postulaciones");

        migrationBuilder.DropTable(
            name: "rubricas",
            schema: "postulaciones");
    }
}
