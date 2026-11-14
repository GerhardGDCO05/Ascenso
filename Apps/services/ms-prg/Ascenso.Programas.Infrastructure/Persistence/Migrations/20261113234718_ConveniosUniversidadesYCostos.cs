using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ascenso.Programas.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ConveniosUniversidadesYCostos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "universidades_aliadas",
                schema: "programas",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    codigo_institucional = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    nombre = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    direccion = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: false),
                    contacto_enlace_nombre = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    contacto_enlace_email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    contacto_enlace_telefono = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    cuenta_banco = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    cuenta_tipo = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    cuenta_numero_cifrado = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    cuenta_rif_titular = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    usuario_enlace_id = table.Column<Guid>(type: "uuid", nullable: true),
                    estado = table.Column<string>(type: "varchar", maxLength: 20, nullable: false),
                    fecha_creacion = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    usuario_creacion_id = table.Column<Guid>(type: "uuid", nullable: false),
                    fecha_ultima_modificacion = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    usuario_modificacion_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_universidades_aliadas", x => x.id);
                    table.CheckConstraint("ck_universidad_aliada_estado_valido", "estado IN ('ACTIVA', 'INACTIVA')");
                });

            migrationBuilder.CreateTable(
                name: "carreras_elegibles",
                schema: "programas",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    universidad_aliada_id = table.Column<Guid>(type: "uuid", nullable: false),
                    codigo_carrera = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    nombre = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    duracion_semestres = table.Column<int>(type: "integer", nullable: false),
                    activa = table.Column<bool>(type: "boolean", nullable: false),
                    fecha_creacion = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    usuario_creacion_id = table.Column<Guid>(type: "uuid", nullable: false),
                    fecha_ultima_modificacion = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    usuario_modificacion_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_carreras_elegibles", x => x.id);
                    table.CheckConstraint("ck_carrera_elegible_duracion_positiva", "duracion_semestres > 0");
                    table.ForeignKey(
                        name: "fk_carreras_elegibles_universidad",
                        column: x => x.universidad_aliada_id,
                        principalSchema: "programas",
                        principalTable: "universidades_aliadas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "convenios_universidad",
                schema: "programas",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    universidad_aliada_id = table.Column<Guid>(type: "uuid", nullable: false),
                    numero_convenio = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    terminos_resumen = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    fecha_inicio = table.Column<DateOnly>(type: "date", nullable: false),
                    fecha_fin = table.Column<DateOnly>(type: "date", nullable: false),
                    url_documento_pdf_firebase = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    permite_intercambio_automatico_datos = table.Column<bool>(type: "boolean", nullable: false),
                    estado = table.Column<string>(type: "varchar", maxLength: 20, nullable: false),
                    fecha_creacion = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    usuario_creacion_id = table.Column<Guid>(type: "uuid", nullable: false),
                    fecha_ultima_modificacion = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    usuario_modificacion_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_convenios_universidad", x => x.id);
                    table.CheckConstraint("ck_convenio_estado_valido", "estado IN ('VIGENTE', 'VENCIDO', 'SUSPENDIDO')");
                    table.CheckConstraint("ck_convenio_fechas_validas", "fecha_fin > fecha_inicio");
                    table.ForeignKey(
                        name: "fk_convenios_universidad_universidad",
                        column: x => x.universidad_aliada_id,
                        principalSchema: "programas",
                        principalTable: "universidades_aliadas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "costos_matricula_referencia",
                schema: "programas",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    carrera_elegible_id = table.Column<Guid>(type: "uuid", nullable: false),
                    periodo_academico_id = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    numero_version = table.Column<int>(type: "integer", nullable: false),
                    monto_referencia_matricula = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    moneda = table.Column<string>(type: "character(3)", fixedLength: true, maxLength: 3, nullable: false),
                    fecha_vigencia_desde = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    fecha_vigencia_hasta = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    estado = table.Column<string>(type: "varchar", maxLength: 20, nullable: false),
                    fecha_creacion = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    usuario_creacion_id = table.Column<Guid>(type: "uuid", nullable: false),
                    fecha_ultima_modificacion = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    usuario_modificacion_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_costos_matricula_referencia", x => x.id);
                    table.CheckConstraint("ck_costo_matricula_estado_valido", "estado IN ('VIGENTE', 'HISTORICO')");
                    table.CheckConstraint("ck_costo_matricula_monto_positivo", "monto_referencia_matricula > 0");
                    table.CheckConstraint("ck_costo_matricula_version_positiva", "numero_version > 0");
                    table.ForeignKey(
                        name: "fk_costos_matricula_carrera",
                        column: x => x.carrera_elegible_id,
                        principalSchema: "programas",
                        principalTable: "carreras_elegibles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ux_carrera_elegible_universidad_codigo",
                schema: "programas",
                table: "carreras_elegibles",
                columns: new[] { "universidad_aliada_id", "codigo_carrera" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_convenio_universidad_numero",
                schema: "programas",
                table: "convenios_universidad",
                columns: new[] { "universidad_aliada_id", "numero_convenio" });

            migrationBuilder.CreateIndex(
                name: "ux_costo_matricula_carrera_periodo_version",
                schema: "programas",
                table: "costos_matricula_referencia",
                columns: new[] { "carrera_elegible_id", "periodo_academico_id", "numero_version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_universidad_aliada_codigo_institucional",
                schema: "programas",
                table: "universidades_aliadas",
                column: "codigo_institucional",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "convenios_universidad",
                schema: "programas");

            migrationBuilder.DropTable(
                name: "costos_matricula_referencia",
                schema: "programas");

            migrationBuilder.DropTable(
                name: "carreras_elegibles",
                schema: "programas");

            migrationBuilder.DropTable(
                name: "universidades_aliadas",
                schema: "programas");
        }
    }
}
