using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ascenso.Postulaciones.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class ListaEsperaPostulaciones : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "entradas_lista_espera",
            schema: "postulaciones",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                convocatoria_id = table.Column<Guid>(type: "uuid", nullable: false),
                programa_beca_id = table.Column<Guid>(type: "uuid", nullable: false),
                postulacion_id = table.Column<Guid>(type: "uuid", nullable: false),
                posicion_orden = table.Column<int>(type: "integer", nullable: false),
                monto_estimado_beca_snapshot = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                estado = table.Column<string>(type: "varchar", maxLength: 30, nullable: false),
                fecha_ingreso = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                fecha_elegibilidad = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                fecha_promocion = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                promovido_por_usuario_id = table.Column<Guid>(type: "uuid", nullable: true),
                motivo_promocion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                fecha_creacion = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                usuario_creacion_id = table.Column<Guid>(type: "uuid", nullable: false),
                fecha_ultima_modificacion = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                usuario_modificacion_id = table.Column<Guid>(type: "uuid", nullable: false),
                version = table.Column<int>(type: "integer", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_entradas_lista_espera", x => x.id);
                table.CheckConstraint("ck_entradas_lista_espera_estado_coherente", "(estado = 'EN_ESPERA' AND fecha_elegibilidad IS NULL AND fecha_promocion IS NULL AND promovido_por_usuario_id IS NULL AND motivo_promocion IS NULL) OR (estado = 'ELEGIBLE_POR_VACANTE' AND fecha_elegibilidad IS NOT NULL AND fecha_promocion IS NULL AND promovido_por_usuario_id IS NULL AND motivo_promocion IS NULL) OR (estado = 'PROMOVIDA' AND fecha_promocion IS NOT NULL AND promovido_por_usuario_id IS NOT NULL AND (motivo_promocion IS NULL OR length(btrim(motivo_promocion)) > 0)) OR estado NOT IN ('EN_ESPERA', 'ELEGIBLE_POR_VACANTE', 'PROMOVIDA')");
                table.CheckConstraint("ck_entradas_lista_espera_estado_valido", "estado IN ('EN_ESPERA', 'ELEGIBLE_POR_VACANTE', 'PROMOVIDA')");
                table.CheckConstraint("ck_entradas_lista_espera_monto_positivo", "monto_estimado_beca_snapshot > 0");
                table.CheckConstraint("ck_entradas_lista_espera_posicion_positiva", "posicion_orden >= 1");
            });

        migrationBuilder.CreateIndex(
            name: "ix_entradas_lista_espera_convocatoria_estado_posicion",
            schema: "postulaciones",
            table: "entradas_lista_espera",
            columns: ["convocatoria_id", "estado", "posicion_orden"]);

        migrationBuilder.CreateIndex(
            name: "ux_entradas_lista_espera_convocatoria_posicion",
            schema: "postulaciones",
            table: "entradas_lista_espera",
            columns: ["convocatoria_id", "posicion_orden"],
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ux_entradas_lista_espera_postulacion",
            schema: "postulaciones",
            table: "entradas_lista_espera",
            column: "postulacion_id",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "entradas_lista_espera",
            schema: "postulaciones");
    }
}
