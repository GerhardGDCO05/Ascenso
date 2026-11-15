using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ascenso.Programas.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ControlPresupuestarioYReservasAtomicas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "cupos_reservados",
                schema: "programas",
                table: "programas_beca",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "presupuesto_ejecutado",
                schema: "programas",
                table: "programas_beca",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "presupuesto_reservado",
                schema: "programas",
                table: "programas_beca",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "movimientos_presupuestarios",
                schema: "programas",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    programa_beca_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tipo_movimiento = table.Column<string>(type: "varchar", maxLength: 35, nullable: false),
                    monto = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    moneda = table.Column<string>(type: "character(3)", fixedLength: true, maxLength: 3, nullable: false),
                    referencia_externa_id = table.Column<Guid>(type: "uuid", nullable: true),
                    periodo_academico_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    descripcion = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    fecha_movimiento = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    fecha_creacion = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    usuario_autor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    correlacion_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_movimientos_presupuestarios", x => x.id);
                    table.CheckConstraint("ck_movimiento_presupuestario_monto_no_negativo", "monto >= 0");
                    table.CheckConstraint("ck_movimiento_presupuestario_tipo_valido", "tipo_movimiento IN ('ASIGNACION_DONANTE', 'REAJUSTE_DONANTE', 'RESERVA_CUPO', 'CONFIRMACION_DESEMBOLSO', 'LIBERACION_RESERVA', 'CIERRE_PERIODO')");
                    table.ForeignKey(
                        name: "fk_movimientos_presupuestarios_programa",
                        column: x => x.programa_beca_id,
                        principalSchema: "programas",
                        principalTable: "programas_beca",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "reservas_presupuestarias",
                schema: "programas",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    programa_beca_id = table.Column<Guid>(type: "uuid", nullable: false),
                    referencia_externa_id = table.Column<Guid>(type: "uuid", nullable: false),
                    periodo_academico_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    monto = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    moneda = table.Column<string>(type: "character(3)", fixedLength: true, maxLength: 3, nullable: false),
                    descripcion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    estado = table.Column<string>(type: "varchar", maxLength: 20, nullable: false),
                    motivo_liberacion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    fecha_reserva = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    fecha_resolucion = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    fecha_creacion = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    usuario_creacion_id = table.Column<Guid>(type: "uuid", nullable: false),
                    fecha_ultima_modificacion = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    usuario_modificacion_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_reservas_presupuestarias", x => x.id);
                    table.CheckConstraint("ck_reserva_presupuestaria_estado_valido", "estado IN ('RESERVADA', 'CONFIRMADA', 'LIBERADA')");
                    table.CheckConstraint("ck_reserva_presupuestaria_monto_positivo", "monto > 0");
                    table.ForeignKey(
                        name: "fk_reservas_presupuestarias_programa",
                        column: x => x.programa_beca_id,
                        principalSchema: "programas",
                        principalTable: "programas_beca",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.AddCheckConstraint(
                name: "ck_programa_beca_cupos_dentro_de_limite",
                schema: "programas",
                table: "programas_beca",
                sql: "cupos_reservados + cupos_ocupados <= cupo_total");

            migrationBuilder.AddCheckConstraint(
                name: "ck_programa_beca_cupos_reservados_no_negativo",
                schema: "programas",
                table: "programas_beca",
                sql: "cupos_reservados >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_programa_beca_presupuesto_dentro_de_limite",
                schema: "programas",
                table: "programas_beca",
                sql: "presupuesto_reservado + presupuesto_ejecutado <= presupuesto_total_asignado");

            migrationBuilder.AddCheckConstraint(
                name: "ck_programa_beca_presupuesto_ejecutado_no_negativo",
                schema: "programas",
                table: "programas_beca",
                sql: "presupuesto_ejecutado >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_programa_beca_presupuesto_reservado_no_negativo",
                schema: "programas",
                table: "programas_beca",
                sql: "presupuesto_reservado >= 0");

            migrationBuilder.CreateIndex(
                name: "ix_movimiento_presupuestario_programa_fecha",
                schema: "programas",
                table: "movimientos_presupuestarios",
                columns: new[] { "programa_beca_id", "fecha_movimiento" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_movimiento_presupuestario_programa_periodo",
                schema: "programas",
                table: "movimientos_presupuestarios",
                columns: new[] { "programa_beca_id", "periodo_academico_id" });

            migrationBuilder.CreateIndex(
                name: "ix_reserva_presupuestaria_programa_periodo_estado",
                schema: "programas",
                table: "reservas_presupuestarias",
                columns: new[] { "programa_beca_id", "periodo_academico_id", "estado" });

            migrationBuilder.CreateIndex(
                name: "ux_reserva_presupuestaria_programa_referencia",
                schema: "programas",
                table: "reservas_presupuestarias",
                columns: new[] { "programa_beca_id", "referencia_externa_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "movimientos_presupuestarios",
                schema: "programas");

            migrationBuilder.DropTable(
                name: "reservas_presupuestarias",
                schema: "programas");

            migrationBuilder.DropCheckConstraint(
                name: "ck_programa_beca_cupos_dentro_de_limite",
                schema: "programas",
                table: "programas_beca");

            migrationBuilder.DropCheckConstraint(
                name: "ck_programa_beca_cupos_reservados_no_negativo",
                schema: "programas",
                table: "programas_beca");

            migrationBuilder.DropCheckConstraint(
                name: "ck_programa_beca_presupuesto_dentro_de_limite",
                schema: "programas",
                table: "programas_beca");

            migrationBuilder.DropCheckConstraint(
                name: "ck_programa_beca_presupuesto_ejecutado_no_negativo",
                schema: "programas",
                table: "programas_beca");

            migrationBuilder.DropCheckConstraint(
                name: "ck_programa_beca_presupuesto_reservado_no_negativo",
                schema: "programas",
                table: "programas_beca");

            migrationBuilder.DropColumn(
                name: "cupos_reservados",
                schema: "programas",
                table: "programas_beca");

            migrationBuilder.DropColumn(
                name: "presupuesto_ejecutado",
                schema: "programas",
                table: "programas_beca");

            migrationBuilder.DropColumn(
                name: "presupuesto_reservado",
                schema: "programas",
                table: "programas_beca");
        }
    }
}
