using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ascenso.Programas.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RegistroYConfiguracionProgramasBeca : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "programas_beca",
                schema: "programas",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    codigo = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    nombre = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    descripcion = table.Column<string>(type: "character varying(1500)", maxLength: 1500, nullable: false),
                    modalidad_cobertura = table.Column<string>(type: "varchar", maxLength: 20, nullable: false),
                    porcentaje_cobertura = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    monto_fijo_periodo = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    moneda_cobertura = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    cupo_total = table.Column<int>(type: "integer", nullable: false),
                    cupos_ocupados = table.Column<int>(type: "integer", nullable: false),
                    duracion_periodos = table.Column<int>(type: "integer", nullable: false),
                    periodo_inicial_id = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    vigencia_desde = table.Column<DateOnly>(type: "date", nullable: false),
                    vigencia_hasta = table.Column<DateOnly>(type: "date", nullable: false),
                    permite_acumulacion = table.Column<bool>(type: "boolean", nullable: false),
                    requiere_compromiso_firmado = table.Column<bool>(type: "boolean", nullable: false),
                    terminos_compromiso_especial = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    presupuesto_total_asignado = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    moneda = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    estado = table.Column<string>(type: "varchar", maxLength: 20, nullable: false),
                    criterio_promedio_minimo_ingreso = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    criterio_edad_minima = table.Column<int>(type: "integer", nullable: true),
                    criterio_edad_maxima = table.Column<int>(type: "integer", nullable: true),
                    criterio_tope_ingreso_familiar = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    criterio_universidades_elegibles_ids = table.Column<string>(type: "jsonb", nullable: true),
                    criterio_carreras_elegibles_ids = table.Column<string>(type: "jsonb", nullable: true),
                    permanencia_promedio_minimo_renovacion = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    permanencia_materias_minimas_aprobadas = table.Column<int>(type: "integer", nullable: true),
                    permanencia_maximo_materias_reprobadas = table.Column<int>(type: "integer", nullable: true),
                    permanencia_horas_servicio_periodo = table.Column<int>(type: "integer", nullable: true),
                    permanencia_frecuencia_reportes = table.Column<string>(type: "varchar", maxLength: 20, nullable: true),
                    permanencia_consecuencia_incumplimiento = table.Column<string>(type: "varchar", maxLength: 30, nullable: true),
                    permanencia_margen_riesgo_promedio = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    permanencia_duracion_maxima_pausa_periodos = table.Column<int>(type: "integer", nullable: true),
                    permanencia_permite_renovacion_tardia = table.Column<bool>(type: "boolean", nullable: true),
                    fecha_creacion = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    usuario_creacion_id = table.Column<Guid>(type: "uuid", nullable: false),
                    fecha_ultima_modificacion = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    usuario_modificacion_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_programas_beca", x => x.id);
                    table.CheckConstraint("ck_programa_beca_cupo_positivo", "cupo_total > 0");
                    table.CheckConstraint("ck_programa_beca_cupos_ocupados_no_negativo", "cupos_ocupados >= 0");
                    table.CheckConstraint("ck_programa_beca_duracion_positiva", "duracion_periodos > 0");
                    table.CheckConstraint("ck_programa_beca_estado_valido", "estado IN ('BORRADOR', 'CONFIGURADO', 'ACTIVO', 'CERRADO', 'ARCHIVADO')");
                    table.CheckConstraint("ck_programa_beca_fechas_vigencia", "vigencia_hasta > vigencia_desde");
                    table.CheckConstraint("ck_programa_beca_presupuesto_no_negativo", "presupuesto_total_asignado >= 0");
                });

            migrationBuilder.CreateTable(
                name: "asignaciones_fondo_programa",
                schema: "programas",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    programa_beca_id = table.Column<Guid>(type: "uuid", nullable: false),
                    donante_id = table.Column<Guid>(type: "uuid", nullable: false),
                    periodo_academico_id = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    monto_asignado = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    estado = table.Column<string>(type: "varchar", maxLength: 20, nullable: false),
                    fecha_asignacion = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    autorizado_por_usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    fecha_creacion = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    usuario_creacion_id = table.Column<Guid>(type: "uuid", nullable: false),
                    fecha_ultima_modificacion = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    usuario_modificacion_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_asignaciones_fondo_programa", x => x.id);
                    table.CheckConstraint("ck_asignacion_fondo_estado_valido", "estado IN ('ASIGNADA', 'DESASIGNADA')");
                    table.CheckConstraint("ck_asignacion_fondo_monto_no_negativo", "monto_asignado >= 0");
                    table.ForeignKey(
                        name: "fk_asignaciones_fondo_programa_programa",
                        column: x => x.programa_beca_id,
                        principalSchema: "programas",
                        principalTable: "programas_beca",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_asignacion_fondo_programa_donante_periodo",
                schema: "programas",
                table: "asignaciones_fondo_programa",
                columns: new[] { "programa_beca_id", "donante_id", "periodo_academico_id" });

            migrationBuilder.CreateIndex(
                name: "ux_programa_beca_codigo",
                schema: "programas",
                table: "programas_beca",
                column: "codigo",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "asignaciones_fondo_programa",
                schema: "programas");

            migrationBuilder.DropTable(
                name: "programas_beca",
                schema: "programas");
        }
    }
}
