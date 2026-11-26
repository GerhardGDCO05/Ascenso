using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ascenso.Becas.Infrastructure.Persistence.Migraciones
{
    /// <inheritdoc />
    public partial class SolicitudRenovacion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "solicitud_renovacion",
                schema: "becas",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    beca_id = table.Column<Guid>(type: "uuid", nullable: false),
                    periodo_origen_id = table.Column<Guid>(type: "uuid", nullable: false),
                    periodo_solicitado_id = table.Column<Guid>(type: "uuid", nullable: false),
                    fecha_solicitud = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    fecha_limite_ventana_renovacion = table.Column<DateOnly>(type: "date", nullable: false),
                    cumple_rendimiento_academico = table.Column<bool>(type: "boolean", nullable: false),
                    tiene_presupuesto_disponible = table.Column<bool>(type: "boolean", nullable: false),
                    monto_reservado_renovacion = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    reserva_presupuesto_id = table.Column<Guid>(type: "uuid", nullable: true),
                    estado = table.Column<string>(type: "varchar", maxLength: 30, nullable: false),
                    motivo_decision = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    evaluado_por_coordinador_id = table.Column<Guid>(type: "uuid", nullable: true),
                    fecha_decision = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    referencia_job_vencimiento_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    fecha_creacion = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    usuario_creacion_id = table.Column<Guid>(type: "uuid", nullable: false),
                    fecha_ultima_modificacion = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    usuario_modificacion_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_solicitud_renovacion", x => x.id);
                    table.CheckConstraint("ck_solicitud_renovacion_elegibilidad", "cumple_rendimiento_academico AND tiene_presupuesto_disponible");
                    table.CheckConstraint("ck_solicitud_renovacion_estado", "estado IN ('SOLICITADA_EN_CURSO', 'APROBADA', 'RECHAZADA', 'EXPIRADA_CONCLUIDA')");
                    table.CheckConstraint("ck_solicitud_renovacion_fechas", "fecha_limite_ventana_renovacion >= (fecha_solicitud AT TIME ZONE 'UTC')::date");
                    table.CheckConstraint("ck_solicitud_renovacion_monto_reserva", "(reserva_presupuesto_id IS NULL AND monto_reservado_renovacion = 0) OR (reserva_presupuesto_id IS NOT NULL AND monto_reservado_renovacion > 0)");
                    table.CheckConstraint("ck_solicitud_renovacion_periodos", "periodo_origen_id <> periodo_solicitado_id");
                    table.ForeignKey(
                        name: "fk_solicitud_renovacion_beca",
                        column: x => x.beca_id,
                        principalSchema: "becas",
                        principalTable: "beca",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ux_solicitud_renovacion_beca_periodo",
                schema: "becas",
                table: "solicitud_renovacion",
                columns: new[] { "beca_id", "periodo_solicitado_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "solicitud_renovacion",
                schema: "becas");
        }
    }
}
