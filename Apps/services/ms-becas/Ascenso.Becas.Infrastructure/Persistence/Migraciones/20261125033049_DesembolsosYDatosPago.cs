using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ascenso.Becas.Infrastructure.Persistence.Migraciones
{
    /// <inheritdoc />
    public partial class DesembolsosYDatosPago : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "calendario_desembolso",
                schema: "becas",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    beca_id = table.Column<Guid>(type: "uuid", nullable: false),
                    periodo_academico_id = table.Column<Guid>(type: "uuid", nullable: false),
                    frecuencia = table.Column<string>(type: "varchar", nullable: false),
                    destinatario_predeterminado = table.Column<string>(type: "varchar", nullable: false),
                    monto_maximo_aprobado_periodo = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    suma_cuotas_programadas = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    numero_cuotas = table.Column<int>(type: "integer", nullable: false),
                    fecha_inicio = table.Column<DateOnly>(type: "date", nullable: false),
                    fecha_fin = table.Column<DateOnly>(type: "date", nullable: false),
                    estado_plan = table.Column<string>(type: "varchar", nullable: false),
                    creado_por_usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    fecha_creacion = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    usuario_creacion_id = table.Column<Guid>(type: "uuid", nullable: false),
                    fecha_ultima_modificacion = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    usuario_modificacion_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_calendario_desembolso", x => x.id);
                    table.CheckConstraint("ck_calendario_destinatario", "destinatario_predeterminado IN ('BECARIO', 'INSTITUCION')");
                    table.CheckConstraint("ck_calendario_estado", "estado_plan IN ('PROGRAMADO', 'SUSPENDIDO', 'COMPLETADO', 'CANCELADO')");
                    table.CheckConstraint("ck_calendario_fechas", "fecha_fin >= fecha_inicio AND numero_cuotas > 0");
                    table.CheckConstraint("ck_calendario_frecuencia", "frecuencia IN ('MENSUAL', 'SEMESTRAL')");
                    table.CheckConstraint("ck_calendario_montos", "monto_maximo_aprobado_periodo >= 0 AND suma_cuotas_programadas >= 0 AND suma_cuotas_programadas <= monto_maximo_aprobado_periodo");
                });

            migrationBuilder.CreateTable(
                name: "datos_pago_becario",
                schema: "becas",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    postulante_estudiante_id = table.Column<Guid>(type: "uuid", nullable: false),
                    beca_id = table.Column<Guid>(type: "uuid", nullable: false),
                    nombre_banco = table.Column<string>(type: "text", nullable: false),
                    codigo_banco = table.Column<string>(type: "text", nullable: false),
                    tipo_cuenta = table.Column<string>(type: "varchar", nullable: false),
                    numero_cuenta_cifrado_aes_gcm = table.Column<string>(type: "text", nullable: false),
                    documento_titular = table.Column<string>(type: "text", nullable: false),
                    estado = table.Column<string>(type: "varchar", nullable: false),
                    modificado_por_usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    autorizacion_otp_modificacion_id = table.Column<Guid>(type: "uuid", nullable: true),
                    fecha_creacion = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    usuario_creacion_id = table.Column<Guid>(type: "uuid", nullable: false),
                    fecha_ultima_modificacion = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    usuario_modificacion_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_datos_pago_becario", x => x.id);
                    table.CheckConstraint("ck_datos_pago_cifrado", "numero_cuenta_cifrado_aes_gcm LIKE 'v1:%'");
                    table.CheckConstraint("ck_datos_pago_estado", "estado IN ('ACTIVO', 'REEMPLAZADO')");
                    table.CheckConstraint("ck_datos_pago_tipo_cuenta", "tipo_cuenta IN ('AHORRO', 'CORRIENTE')");
                });

            migrationBuilder.CreateTable(
                name: "desembolso",
                schema: "becas",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    calendario_desembolso_id = table.Column<Guid>(type: "uuid", nullable: false),
                    beca_id = table.Column<Guid>(type: "uuid", nullable: false),
                    numero_cuota = table.Column<int>(type: "integer", nullable: false),
                    monto_programado = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    monto_ejecutado = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    fecha_programada = table.Column<DateOnly>(type: "date", nullable: false),
                    fecha_ejecucion_efectiva = table.Column<DateOnly>(type: "date", nullable: true),
                    destinatario = table.Column<string>(type: "varchar", nullable: false),
                    datos_pago_becario_id = table.Column<Guid>(type: "uuid", nullable: true),
                    cuenta_bancaria_destino_snapshot = table.Column<string>(type: "text", nullable: true),
                    metodo_pago = table.Column<string>(type: "varchar", nullable: true),
                    estado = table.Column<string>(type: "varchar", nullable: false),
                    intentos_ejecucion = table.Column<int>(type: "integer", nullable: false),
                    referencia_bancaria = table.Column<string>(type: "text", nullable: true),
                    observacion_incidencia_bancaria = table.Column<string>(type: "text", nullable: true),
                    registrado_por_usuario_id = table.Column<Guid>(type: "uuid", nullable: true),
                    confirmado_por_usuario_id = table.Column<Guid>(type: "uuid", nullable: true),
                    autorizacion_otp_confirmacion_id = table.Column<Guid>(type: "uuid", nullable: true),
                    fecha_confirmacion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    suspension_desembolso_id = table.Column<Guid>(type: "uuid", nullable: true),
                    pausa_aplazamiento_id = table.Column<Guid>(type: "uuid", nullable: true),
                    desembolso_origen_reintento_id = table.Column<Guid>(type: "uuid", nullable: true),
                    es_vigente = table.Column<bool>(type: "boolean", nullable: false),
                    fecha_creacion = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    usuario_creacion_id = table.Column<Guid>(type: "uuid", nullable: false),
                    fecha_ultima_modificacion = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    usuario_modificacion_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_desembolso", x => x.id);
                    table.CheckConstraint("ck_desembolso_destinatario", "destinatario IN ('BECARIO', 'INSTITUCION')");
                    table.CheckConstraint("ck_desembolso_estado", "estado IN ('PROGRAMADO', 'EN_PROCESO', 'PAGADO_CONFIRMADO', 'FALLIDO', 'DEVUELTO', 'SUSPENDIDO', 'CANCELADO')");
                    table.CheckConstraint("ck_desembolso_intentos", "intentos_ejecucion >= 0");
                    table.CheckConstraint("ck_desembolso_metodo", "metodo_pago IS NULL OR metodo_pago IN ('TRANSFERENCIA_BANCARIA')");
                    table.CheckConstraint("ck_desembolso_montos", "monto_programado > 0 AND (monto_ejecutado IS NULL OR (monto_ejecutado > 0 AND monto_ejecutado <= monto_programado))");
                    table.ForeignKey(
                        name: "FK_desembolso_calendario_desembolso_calendario_desembolso_id",
                        column: x => x.calendario_desembolso_id,
                        principalSchema: "becas",
                        principalTable: "calendario_desembolso",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "incidencia_desembolso",
                schema: "becas",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    desembolso_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tipo = table.Column<string>(type: "varchar", nullable: false),
                    motivo = table.Column<string>(type: "text", nullable: false),
                    observacion_banco = table.Column<string>(type: "text", nullable: false),
                    estado = table.Column<string>(type: "varchar", nullable: false),
                    fecha_registro = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    registrado_por_usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    resuelto_por_usuario_id = table.Column<Guid>(type: "uuid", nullable: true),
                    fecha_resolucion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    fecha_creacion = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    usuario_creacion_id = table.Column<Guid>(type: "uuid", nullable: false),
                    fecha_ultima_modificacion = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    usuario_modificacion_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_incidencia_desembolso", x => x.id);
                    table.CheckConstraint("ck_incidencia_estado", "estado IN ('ABIERTA', 'RESUELTA')");
                    table.CheckConstraint("ck_incidencia_observacion", "length(trim(observacion_banco)) > 0");
                    table.CheckConstraint("ck_incidencia_tipo", "tipo IN ('FALLO_BANCARIO', 'DEVOLUCION_BANCARIA')");
                    table.ForeignKey(
                        name: "FK_incidencia_desembolso_desembolso_desembolso_id",
                        column: x => x.desembolso_id,
                        principalSchema: "becas",
                        principalTable: "desembolso",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_calendario_desembolso_beca_id_periodo_academico_id",
                schema: "becas",
                table: "calendario_desembolso",
                columns: new[] { "beca_id", "periodo_academico_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_datos_pago_becario_beca_id",
                schema: "becas",
                table: "datos_pago_becario",
                column: "beca_id",
                unique: true,
                filter: "estado = 'ACTIVO'");

            migrationBuilder.CreateIndex(
                name: "IX_desembolso_calendario_desembolso_id_numero_cuota",
                schema: "becas",
                table: "desembolso",
                columns: new[] { "calendario_desembolso_id", "numero_cuota" },
                unique: true,
                filter: "es_vigente = true");

            migrationBuilder.CreateIndex(
                name: "IX_desembolso_calendario_desembolso_id_numero_cuota_intentos_e~",
                schema: "becas",
                table: "desembolso",
                columns: new[] { "calendario_desembolso_id", "numero_cuota", "intentos_ejecucion" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_incidencia_desembolso_desembolso_id",
                schema: "becas",
                table: "incidencia_desembolso",
                column: "desembolso_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "datos_pago_becario",
                schema: "becas");

            migrationBuilder.DropTable(
                name: "incidencia_desembolso",
                schema: "becas");

            migrationBuilder.DropTable(
                name: "desembolso",
                schema: "becas");

            migrationBuilder.DropTable(
                name: "calendario_desembolso",
                schema: "becas");
        }
    }
}
