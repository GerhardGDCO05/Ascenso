using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ascenso.Postulaciones.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class ExcepcionEvaluadorUnico : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "excepciones_evaluador_unico",
            schema: "postulaciones",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                postulacion_id = table.Column<Guid>(type: "uuid", nullable: false),
                justificacion = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: false),
                fecha_autorizacion = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                usuario_autorizador_id = table.Column<Guid>(type: "uuid", nullable: false),
                correlacion_id = table.Column<Guid>(type: "uuid", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_excepciones_evaluador_unico", x => x.id);
                table.CheckConstraint("ck_excepciones_justificacion_no_vacia", "char_length(btrim(justificacion)) >= 10");
            });

        migrationBuilder.CreateIndex(
            name: "ux_excepciones_evaluador_unico_postulacion",
            schema: "postulaciones",
            table: "excepciones_evaluador_unico",
            column: "postulacion_id",
            unique: true);

        migrationBuilder.Sql(
            """
            CREATE OR REPLACE FUNCTION postulaciones.fn_excepciones_evaluador_unico_inmutable()
            RETURNS trigger
            LANGUAGE plpgsql
            AS $$
            BEGIN
                RAISE EXCEPTION 'Los registros de excepciones_evaluador_unico son inmutables y no pueden modificarse ni eliminarse.';
            END;
            $$;

            CREATE TRIGGER trg_excepciones_evaluador_unico_inmutable
            BEFORE UPDATE OR DELETE ON postulaciones.excepciones_evaluador_unico
            FOR EACH ROW
            EXECUTE FUNCTION postulaciones.fn_excepciones_evaluador_unico_inmutable();
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DROP TRIGGER IF EXISTS trg_excepciones_evaluador_unico_inmutable ON postulaciones.excepciones_evaluador_unico;
            DROP FUNCTION IF EXISTS postulaciones.fn_excepciones_evaluador_unico_inmutable();
            """);

        migrationBuilder.DropTable(
            name: "excepciones_evaluador_unico",
            schema: "postulaciones");
    }
}
