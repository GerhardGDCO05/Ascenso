using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ascenso.Becas.Infrastructure.Persistence.Migraciones
{
    /// <inheritdoc />
    public partial class SeguimientoAcademico : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "reporte_academico",
                schema: "becas",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    beca_id = table.Column<Guid>(type: "uuid", nullable: false),
                    periodo_academico_id = table.Column<Guid>(type: "uuid", nullable: false),
                    promedio_periodo = table.Column<decimal>(type: "numeric(4,2)", precision: 4, scale: 2, nullable: false),
                    materias_aprobadas = table.Column<int>(type: "integer", nullable: false),
                    materias_reprobadas = table.Column<int>(type: "integer", nullable: false),
                    materias_cursando = table.Column<int>(type: "integer", nullable: false),
                    creditos_aprobados = table.Column<int>(type: "integer", nullable: false),
                    horas_servicio_cumplidas = table.Column<int>(type: "integer", nullable: true),
                    url_comprobante_servicio = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    hash_comprobante_servicio_sha256 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    estado_matricula = table.Column<string>(type: "varchar", maxLength: 30, nullable: false),
                    url_comprobante_notas_firebase = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    hash_comprobante_sha256 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    tamanio_comprobante_bytes = table.Column<long>(type: "bigint", nullable: false),
                    tipo_mime_comprobante = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    fuente_entrega = table.Column<string>(type: "varchar", maxLength: 20, nullable: false),
                    universidad_aliada_id = table.Column<Guid>(type: "uuid", nullable: false),
                    fecha_entrega = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    fecha_limite_periodo = table.Column<DateOnly>(type: "date", nullable: false),
                    entrega_tardia = table.Column<bool>(type: "boolean", nullable: false),
                    estado = table.Column<string>(type: "varchar", maxLength: 20, nullable: false),
                    revisado_por_usuario_id = table.Column<Guid>(type: "uuid", nullable: true),
                    fecha_revision = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    fecha_creacion = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    usuario_creacion_id = table.Column<Guid>(type: "uuid", nullable: false),
                    fecha_ultima_modificacion = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    usuario_modificacion_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_reporte_academico", x => x.id);
                    table.CheckConstraint("ck_reporte_academico_estado", "estado IN ('ENTREGADO', 'EN_REVISION', 'APROBADO', 'RECHAZADO')");
                    table.CheckConstraint("ck_reporte_academico_estado_matricula", "estado_matricula IN ('MATRICULADO', 'NO_MATRICULADO', 'RETIRADO')");
                    table.CheckConstraint("ck_reporte_academico_fuente", "fuente_entrega IN ('BECARIO', 'UNIVERSIDAD')");
                    table.CheckConstraint("ck_reporte_academico_horas", "horas_servicio_cumplidas IS NULL OR horas_servicio_cumplidas >= 0");
                    table.CheckConstraint("ck_reporte_academico_materias", "materias_aprobadas >= 0 AND materias_reprobadas >= 0 AND materias_cursando >= 0 AND creditos_aprobados >= 0");
                    table.CheckConstraint("ck_reporte_academico_promedio", "promedio_periodo >= 0 AND promedio_periodo <= 20");
                    table.ForeignKey(
                        name: "fk_reporte_academico_beca",
                        column: x => x.beca_id,
                        principalSchema: "becas",
                        principalTable: "beca",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "evaluacion_permanencia",
                schema: "becas",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    reporte_academico_id = table.Column<Guid>(type: "uuid", nullable: false),
                    beca_id = table.Column<Guid>(type: "uuid", nullable: false),
                    fecha_evaluacion = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    cumple_condiciones = table.Column<bool>(type: "boolean", nullable: false),
                    consecuencia_recomendada = table.Column<string>(type: "varchar", maxLength: 30, nullable: true),
                    consecuencia_aplicada = table.Column<string>(type: "varchar", maxLength: 30, nullable: true),
                    estado_confirmacion = table.Column<string>(type: "varchar", maxLength: 30, nullable: false),
                    motivo_decision = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    siguiente_paso_becario = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    evaluado_por_coordinador_id = table.Column<Guid>(type: "uuid", nullable: true),
                    fecha_creacion = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    usuario_creacion_id = table.Column<Guid>(type: "uuid", nullable: false),
                    fecha_ultima_modificacion = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    usuario_modificacion_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_evaluacion_permanencia", x => x.id);
                    table.CheckConstraint("ck_evaluacion_permanencia_aplicada", "consecuencia_aplicada IS NULL OR consecuencia_aplicada IN ('LLAMADO_ATENCION', 'SUSPENSION', 'REVOCACION')");
                    table.CheckConstraint("ck_evaluacion_permanencia_estado", "estado_confirmacion IN ('PENDIENTE_CONFIRMACION', 'CONFIRMADA', 'RECHAZADA')");
                    table.CheckConstraint("ck_evaluacion_permanencia_recomendada", "consecuencia_recomendada IS NULL OR consecuencia_recomendada IN ('LLAMADO_ATENCION', 'SUSPENSION', 'REVOCACION')");
                    table.ForeignKey(
                        name: "fk_evaluacion_permanencia_reporte",
                        column: x => x.reporte_academico_id,
                        principalSchema: "becas",
                        principalTable: "reporte_academico",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "condicion_permanencia_evaluada",
                schema: "becas",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    condicion = table.Column<string>(type: "varchar", maxLength: 40, nullable: false),
                    valor_requerido = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    valor_obtenido = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    cumple = table.Column<bool>(type: "boolean", nullable: false),
                    evaluacion_permanencia_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_condicion_permanencia_evaluada", x => x.id);
                    table.CheckConstraint("ck_condicion_permanencia_evaluada_tipo", "condicion IN ('PROMEDIO_MINIMO', 'MATERIAS_MINIMAS_APROBADAS', 'MAXIMO_MATERIAS_REPROBADAS', 'HORAS_SERVICIO_POR_PERIODO')");
                    table.ForeignKey(
                        name: "FK_condicion_permanencia_evaluada_evaluacion_permanencia_evalu~",
                        column: x => x.evaluacion_permanencia_id,
                        principalSchema: "becas",
                        principalTable: "evaluacion_permanencia",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ux_condicion_permanencia_evaluada_tipo",
                schema: "becas",
                table: "condicion_permanencia_evaluada",
                columns: new[] { "evaluacion_permanencia_id", "condicion" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_evaluacion_permanencia_reporte",
                schema: "becas",
                table: "evaluacion_permanencia",
                column: "reporte_academico_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_reporte_academico_beca_periodo",
                schema: "becas",
                table: "reporte_academico",
                columns: new[] { "beca_id", "periodo_academico_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "condicion_permanencia_evaluada",
                schema: "becas");

            migrationBuilder.DropTable(
                name: "evaluacion_permanencia",
                schema: "becas");

            migrationBuilder.DropTable(
                name: "reporte_academico",
                schema: "becas");
        }
    }
}
