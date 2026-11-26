using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ascenso.Becas.Infrastructure.Persistence.Migraciones
{
    /// <inheritdoc />
    public partial class SuspensionDesembolso : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "suspension_desembolso",
                schema: "becas",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    beca_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tipo_suspension = table.Column<string>(type: "varchar", maxLength: 40, nullable: false),
                    motivo_suspension = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    requisitos_para_reanudar = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    fecha_inicio = table.Column<DateOnly>(type: "date", nullable: false),
                    fecha_revision_programada = table.Column<DateOnly>(type: "date", nullable: true),
                    es_indefinida = table.Column<bool>(type: "boolean", nullable: false),
                    estado = table.Column<string>(type: "varchar", maxLength: 20, nullable: false),
                    registrada_por_coordinador_id = table.Column<Guid>(type: "uuid", nullable: false),
                    levantada_por_coordinador_id = table.Column<Guid>(type: "uuid", nullable: true),
                    fecha_resolucion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    fecha_creacion = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    usuario_creacion_id = table.Column<Guid>(type: "uuid", nullable: false),
                    fecha_ultima_modificacion = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    usuario_modificacion_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_suspension_desembolso", x => x.id);
                    table.CheckConstraint("ck_suspension_estado", "estado IN ('VIGENTE', 'LEVANTADA')");
                    table.CheckConstraint("ck_suspension_revision", "(es_indefinida AND fecha_revision_programada IS NULL) OR (NOT es_indefinida AND fecha_revision_programada IS NOT NULL AND fecha_revision_programada >= fecha_inicio)");
                    table.CheckConstraint("ck_suspension_tipo", "tipo_suspension IN ('MANUAL', 'INCUMPLIMIENTO_ACADEMICO', 'PAUSA_APLAZAMIENTO')");
                    table.ForeignKey(
                        name: "fk_suspension_desembolso_beca",
                        column: x => x.beca_id,
                        principalSchema: "becas",
                        principalTable: "beca",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_suspension_desembolso_beca_estado",
                schema: "becas",
                table: "suspension_desembolso",
                columns: new[] { "beca_id", "estado" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "suspension_desembolso",
                schema: "becas");
        }
    }
}
