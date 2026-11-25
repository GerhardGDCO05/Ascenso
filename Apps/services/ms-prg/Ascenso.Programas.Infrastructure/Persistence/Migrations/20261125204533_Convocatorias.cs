using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ascenso.Programas.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Convocatorias : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "convocatorias",
                schema: "programas",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    programa_beca_id = table.Column<Guid>(type: "uuid", nullable: false),
                    periodo_academico_id = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    codigo = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    nombre = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    fecha_apertura = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    fecha_cierre = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    fecha_limite_subsanacion_documentos = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    fecha_inicio_evaluacion = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    fecha_cierre_evaluacion = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    requiere_revision_documental_previa = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    cupos_asignados = table.Column<int>(type: "integer", nullable: false),
                    criterio_promedio_minimo = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    criterio_edad_minima = table.Column<int>(type: "integer", nullable: false),
                    criterio_edad_maxima = table.Column<int>(type: "integer", nullable: false),
                    criterio_tope_ingreso_familiar = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    criterio_universidades_permitidas_ids = table.Column<string>(type: "jsonb", nullable: false),
                    criterio_carreras_permitidas_ids = table.Column<string>(type: "jsonb", nullable: false),
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
                    table.PrimaryKey("pk_convocatorias", x => x.id);
                    table.CheckConstraint("ck_convocatoria_cupos_positivos", "cupos_asignados >= 1");
                    table.CheckConstraint("ck_convocatoria_edad_maxima_valida", "criterio_edad_maxima BETWEEN 0 AND 120");
                    table.CheckConstraint("ck_convocatoria_edad_minima_valida", "criterio_edad_minima BETWEEN 0 AND 120");
                    table.CheckConstraint("ck_convocatoria_edades_ordenadas", "criterio_edad_minima <= criterio_edad_maxima");
                    table.CheckConstraint("ck_convocatoria_estado_valido", "estado IN ('BORRADOR', 'PUBLICADA', 'CERRADA', 'CANCELADA')");
                    table.CheckConstraint("ck_convocatoria_evaluacion_despues_de_subsanacion", "fecha_cierre_evaluacion > fecha_limite_subsanacion_documentos");
                    table.CheckConstraint("ck_convocatoria_evaluacion_ordenada", "fecha_cierre_evaluacion > fecha_inicio_evaluacion");
                    table.CheckConstraint("ck_convocatoria_fechas_ordenadas", "fecha_cierre > fecha_apertura");
                    table.CheckConstraint("ck_convocatoria_promedio_valido", "criterio_promedio_minimo BETWEEN 0 AND 20");
                    table.CheckConstraint("ck_convocatoria_subsanacion_despues_del_cierre", "fecha_limite_subsanacion_documentos >= fecha_cierre");
                    table.CheckConstraint("ck_convocatoria_tope_ingreso_no_negativo", "criterio_tope_ingreso_familiar >= 0");
                    table.ForeignKey(
                        name: "fk_convocatorias_programa_beca",
                        column: x => x.programa_beca_id,
                        principalSchema: "programas",
                        principalTable: "programas_beca",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "documentos_requeridos",
                schema: "programas",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    convocatoria_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tipo_documento = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    nombre = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    descripcion = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    obligatorio = table.Column<bool>(type: "boolean", nullable: false),
                    tipos_mime_permitidos = table.Column<string>(type: "jsonb", nullable: false),
                    tamanio_maximo_mb = table.Column<int>(type: "integer", nullable: false),
                    fecha_creacion = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    usuario_creacion_id = table.Column<Guid>(type: "uuid", nullable: false),
                    fecha_ultima_modificacion = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    usuario_modificacion_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_documentos_requeridos", x => x.id);
                    table.CheckConstraint("ck_documento_requerido_tamanio_positivo", "tamanio_maximo_mb > 0");
                    table.ForeignKey(
                        name: "fk_documentos_requeridos_convocatoria",
                        column: x => x.convocatoria_id,
                        principalSchema: "programas",
                        principalTable: "convocatorias",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_convocatoria_estado_fecha_cierre",
                schema: "programas",
                table: "convocatorias",
                columns: new[] { "estado", "fecha_cierre" });

            migrationBuilder.CreateIndex(
                name: "ix_convocatoria_programa_beca",
                schema: "programas",
                table: "convocatorias",
                column: "programa_beca_id");

            migrationBuilder.CreateIndex(
                name: "ux_convocatoria_codigo",
                schema: "programas",
                table: "convocatorias",
                column: "codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_documento_requerido_convocatoria",
                schema: "programas",
                table: "documentos_requeridos",
                column: "convocatoria_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "documentos_requeridos",
                schema: "programas");

            migrationBuilder.DropTable(
                name: "convocatorias",
                schema: "programas");
        }
    }
}
