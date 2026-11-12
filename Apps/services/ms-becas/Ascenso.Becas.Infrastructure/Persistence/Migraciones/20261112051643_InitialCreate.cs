using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Ascenso.Becas.Infrastructure.Persistence.Migraciones
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "becas");

            migrationBuilder.CreateTable(
                name: "beca",
                schema: "becas",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    cadena_beca_id = table.Column<Guid>(type: "uuid", nullable: false),
                    beca_anterior_id = table.Column<Guid>(type: "uuid", nullable: true),
                    codigo_beca = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    postulante_estudiante_id = table.Column<Guid>(type: "uuid", nullable: false),
                    numero_documento_estudiante = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    postulacion_origen_id = table.Column<Guid>(type: "uuid", nullable: false),
                    programa_beca_id = table.Column<Guid>(type: "uuid", nullable: false),
                    convocatoria_origen_id = table.Column<Guid>(type: "uuid", nullable: false),
                    periodo_otorgamiento_id = table.Column<Guid>(type: "uuid", nullable: false),
                    periodo_vigente_actual_id = table.Column<Guid>(type: "uuid", nullable: false),
                    periodos_cursados = table.Column<int>(type: "integer", nullable: false),
                    duracion_total_periodos = table.Column<int>(type: "integer", nullable: false),
                    cobertura_modalidad = table.Column<string>(type: "varchar", maxLength: 20, nullable: false),
                    cobertura_porcentaje = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    cobertura_monto_fijo_periodo = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    cobertura_moneda = table.Column<string>(type: "character(3)", fixedLength: true, maxLength: 3, nullable: false),
                    costo_matricula_referencia_snapshot_id = table.Column<Guid>(type: "uuid", nullable: false),
                    monto_comprometido_periodo_actual = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    monto_desembolsado_periodo_actual = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    estado = table.Column<string>(type: "varchar", maxLength: 30, nullable: false),
                    tipo_cierre = table.Column<string>(type: "varchar", maxLength: 30, nullable: true),
                    permite_acumulacion = table.Column<bool>(type: "boolean", nullable: false),
                    requiere_compromiso_firmado = table.Column<bool>(type: "boolean", nullable: false),
                    compromiso_firmado_aprobado = table.Column<bool>(type: "boolean", nullable: false),
                    habilitada_para_desembolso = table.Column<bool>(type: "boolean", nullable: false),
                    tiene_renovacion_en_curso = table.Column<bool>(type: "boolean", nullable: false),
                    fecha_otorgamiento = table.Column<DateOnly>(type: "date", nullable: false),
                    fecha_activacion = table.Column<DateOnly>(type: "date", nullable: true),
                    fecha_conclusion_o_revocacion = table.Column<DateOnly>(type: "date", nullable: true),
                    fecha_creacion = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    usuario_creacion_id = table.Column<Guid>(type: "uuid", nullable: false),
                    fecha_ultima_modificacion = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    usuario_modificacion_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_beca", x => x.id);
                    table.CheckConstraint("ck_beca_cobertura_modalidad_valida", "cobertura_modalidad IN ('TOTAL', 'PARCIAL', 'MONTO_FIJO')");
                    table.CheckConstraint("ck_beca_cobertura_porcentaje_valido", "cobertura_porcentaje >= 0 AND cobertura_porcentaje <= 100");
                    table.CheckConstraint("ck_beca_estado_valido", "estado IN ('OTORGADA', 'ACTIVA', 'SUSPENDIDA', 'RENOVADA', 'CONCLUIDA', 'REVOCADA')");
                    table.CheckConstraint("ck_beca_fechas_coherentes", "(fecha_activacion IS NULL OR fecha_activacion >= fecha_otorgamiento)\nAND (fecha_conclusion_o_revocacion IS NULL OR fecha_conclusion_o_revocacion >= fecha_otorgamiento)");
                    table.CheckConstraint("ck_beca_montos_no_negativos", "monto_comprometido_periodo_actual >= 0\nAND monto_desembolsado_periodo_actual >= 0\nAND cobertura_monto_fijo_periodo >= 0");
                    table.CheckConstraint("ck_beca_periodos_validos", "periodos_cursados >= 0\nAND duracion_total_periodos > 0\nAND periodos_cursados <= duracion_total_periodos");
                    table.CheckConstraint("ck_beca_tipo_cierre_valido", "tipo_cierre IS NULL OR tipo_cierre IN ('OFERTA_RECHAZADA', 'OFERTA_EXPIRADA', 'OFERTA_ANULADA', 'REVOCACION_DEFINITIVA')");
                });

            migrationBuilder.CreateTable(
                name: "convenio_ref",
                schema: "becas",
                columns: table => new
                {
                    universidad_aliada_id = table.Column<Guid>(type: "uuid", nullable: false),
                    convenio_id = table.Column<Guid>(type: "uuid", nullable: false),
                    estado = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    fecha_fin = table.Column<DateOnly>(type: "date", nullable: false),
                    ultimo_evento_id = table.Column<Guid>(type: "uuid", nullable: false),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    fecha_ultima_actualizacion = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_convenio_ref", x => x.universidad_aliada_id);
                });

            migrationBuilder.CreateTable(
                name: "costo_matricula_ref",
                schema: "becas",
                columns: table => new
                {
                    costo_matricula_referencia_id = table.Column<Guid>(type: "uuid", nullable: false),
                    carrera_id = table.Column<Guid>(type: "uuid", nullable: false),
                    periodo_academico_id = table.Column<Guid>(type: "uuid", nullable: false),
                    monto_referencia_matricula = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    moneda = table.Column<string>(type: "character(3)", fixedLength: true, maxLength: 3, nullable: false),
                    numero_version = table.Column<int>(type: "integer", nullable: false),
                    fecha_vigencia_desde = table.Column<DateOnly>(type: "date", nullable: false),
                    fecha_vigencia_hasta = table.Column<DateOnly>(type: "date", nullable: true),
                    estado = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ultimo_evento_id = table.Column<Guid>(type: "uuid", nullable: false),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    fecha_ultima_actualizacion = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_costo_matricula_ref", x => x.costo_matricula_referencia_id);
                    table.CheckConstraint("ck_costo_matricula_ref_fechas_coherentes", "fecha_vigencia_hasta IS NULL OR fecha_vigencia_hasta >= fecha_vigencia_desde");
                    table.CheckConstraint("ck_costo_matricula_ref_monto_no_negativo", "monto_referencia_matricula >= 0");
                    table.CheckConstraint("ck_costo_matricula_ref_version_valida", "numero_version > 0");
                });

            migrationBuilder.CreateTable(
                name: "idempotency_request",
                schema: "becas",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    endpoint = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    clave = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    hash_payload = table.Column<string>(type: "character(64)", fixedLength: true, maxLength: 64, nullable: false),
                    estado = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    status_code = table.Column<int>(type: "integer", nullable: true),
                    response_body = table.Column<string>(type: "text", nullable: true),
                    recurso_id = table.Column<Guid>(type: "uuid", nullable: true),
                    creado_en = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    expira_en = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_idempotency_request", x => x.id);
                    table.CheckConstraint("ck_idempotency_request_expiracion_valida", "expira_en > creado_en");
                    table.CheckConstraint("ck_idempotency_request_status_code_valido", "status_code IS NULL OR (status_code >= 100 AND status_code <= 599)");
                });

            migrationBuilder.CreateTable(
                name: "inbox_state",
                schema: "becas",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MessageId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConsumerId = table.Column<Guid>(type: "uuid", nullable: false),
                    LockId = table.Column<Guid>(type: "uuid", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: true),
                    Received = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ReceiveCount = table.Column<int>(type: "integer", nullable: false),
                    ExpirationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Consumed = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Delivered = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastSequenceNumber = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inbox_state", x => x.Id);
                    table.UniqueConstraint("AK_inbox_state_MessageId_ConsumerId", x => new { x.MessageId, x.ConsumerId });
                });

            migrationBuilder.CreateTable(
                name: "outbox_state",
                schema: "becas",
                columns: table => new
                {
                    OutboxId = table.Column<Guid>(type: "uuid", nullable: false),
                    LockId = table.Column<Guid>(type: "uuid", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: true),
                    Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Delivered = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastSequenceNumber = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_outbox_state", x => x.OutboxId);
                });

            migrationBuilder.CreateTable(
                name: "programa_beca_ref",
                schema: "becas",
                columns: table => new
                {
                    programa_beca_id = table.Column<Guid>(type: "uuid", nullable: false),
                    codigo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    nombre = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    politica_cobertura_modalidad = table.Column<string>(type: "varchar", maxLength: 20, nullable: false),
                    politica_cobertura_porcentaje = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    politica_cobertura_monto_fijo_periodo = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    politica_cobertura_moneda = table.Column<string>(type: "character(3)", fixedLength: true, maxLength: 3, nullable: false),
                    condiciones_permanencia_promedio_minimo_renovacion = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    condiciones_permanencia_materias_minimas_aprobadas = table.Column<int>(type: "integer", nullable: false),
                    condiciones_permanencia_maximo_materias_reprobadas = table.Column<int>(type: "integer", nullable: false),
                    condiciones_permanencia_horas_servicio_por_periodo = table.Column<int>(type: "integer", nullable: false),
                    condiciones_permanencia_consecuencia_incumplimiento = table.Column<string>(type: "varchar", maxLength: 30, nullable: false),
                    permite_acumulacion = table.Column<bool>(type: "boolean", nullable: false),
                    requiere_compromiso_firmado = table.Column<bool>(type: "boolean", nullable: false),
                    duracion_periodos = table.Column<int>(type: "integer", nullable: false),
                    estado = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ultimo_evento_id = table.Column<Guid>(type: "uuid", nullable: false),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    fecha_ultima_actualizacion = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_programa_beca_ref", x => x.programa_beca_id);
                    table.CheckConstraint("ck_programa_beca_ref_cobertura_modalidad_valida", "politica_cobertura_modalidad IN ('TOTAL', 'PARCIAL', 'MONTO_FIJO')");
                    table.CheckConstraint("ck_programa_beca_ref_cobertura_porcentaje_valido", "politica_cobertura_porcentaje >= 0 AND politica_cobertura_porcentaje <= 100");
                    table.CheckConstraint("ck_programa_beca_ref_condiciones_validas", "condiciones_permanencia_promedio_minimo_renovacion >= 0\nAND condiciones_permanencia_promedio_minimo_renovacion <= 20\nAND condiciones_permanencia_materias_minimas_aprobadas >= 0\nAND condiciones_permanencia_maximo_materias_reprobadas >= 0\nAND condiciones_permanencia_horas_servicio_por_periodo >= 0\nAND condiciones_permanencia_consecuencia_incumplimiento IN ('LLAMADO_ATENCION', 'SUSPENSION', 'REVOCACION')");
                    table.CheckConstraint("ck_programa_beca_ref_duracion_valida", "duracion_periodos > 0");
                    table.CheckConstraint("ck_programa_beca_ref_montos_no_negativos", "politica_cobertura_monto_fijo_periodo >= 0");
                });

            migrationBuilder.CreateTable(
                name: "compromiso_becario",
                schema: "becas",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    beca_id = table.Column<Guid>(type: "uuid", nullable: false),
                    url_plantilla_generada_firebase = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    url_pdf_firmado_firebase = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    hash_documento_sha256 = table.Column<string>(type: "character(64)", fixedLength: true, maxLength: 64, nullable: true),
                    numero_intento_carga = table.Column<int>(type: "integer", nullable: false),
                    ip_carga_firma = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    fecha_carga_firma = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    estado = table.Column<string>(type: "varchar", maxLength: 30, nullable: false),
                    observaciones_revision = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    revisado_por_coordinador_id = table.Column<Guid>(type: "uuid", nullable: true),
                    fecha_revision = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    fecha_creacion = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    usuario_creacion_id = table.Column<Guid>(type: "uuid", nullable: false),
                    fecha_ultima_modificacion = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    usuario_modificacion_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_compromiso_becario", x => x.id);
                    table.CheckConstraint("ck_compromiso_becario_estado_valido", "estado IN ('PENDIENTE_CARGA', 'EN_REVISION', 'APROBADO', 'RECHAZADO')");
                    table.CheckConstraint("ck_compromiso_becario_intento_valido", "numero_intento_carga >= 0");
                    table.ForeignKey(
                        name: "fk_compromiso_becario_beca",
                        column: x => x.beca_id,
                        principalSchema: "becas",
                        principalTable: "beca",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "historial_estado_beca",
                schema: "becas",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    beca_id = table.Column<Guid>(type: "uuid", nullable: false),
                    oferta_beca_id = table.Column<Guid>(type: "uuid", nullable: true),
                    compromiso_becario_id = table.Column<Guid>(type: "uuid", nullable: true),
                    tipo_entidad = table.Column<string>(type: "varchar", maxLength: 30, nullable: false),
                    estado_beca_anterior = table.Column<string>(type: "varchar", maxLength: 30, nullable: true),
                    estado_beca_nuevo = table.Column<string>(type: "varchar", maxLength: 30, nullable: true),
                    estado_oferta_anterior = table.Column<string>(type: "varchar", maxLength: 30, nullable: true),
                    estado_oferta_nuevo = table.Column<string>(type: "varchar", maxLength: 30, nullable: true),
                    estado_compromiso_anterior = table.Column<string>(type: "varchar", maxLength: 30, nullable: true),
                    estado_compromiso_nuevo = table.Column<string>(type: "varchar", maxLength: 30, nullable: true),
                    fecha_cambio = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    motivo = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    justificacion_detallada = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    autorizacion_otp_id = table.Column<Guid>(type: "uuid", nullable: true),
                    estado_terminal_irreversible = table.Column<bool>(type: "boolean", nullable: false),
                    fecha_creacion = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    usuario_autor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    correlacion_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_historial_estado_beca", x => x.id);
                    table.CheckConstraint("ck_historial_estado_beca_estado_anterior_valido", "(estado_beca_anterior IS NULL OR estado_beca_anterior IN ('OTORGADA', 'ACTIVA', 'SUSPENDIDA', 'RENOVADA', 'CONCLUIDA', 'REVOCADA'))\nAND (estado_oferta_anterior IS NULL OR estado_oferta_anterior IN ('PENDIENTE_RESPUESTA', 'ACEPTADA', 'RECHAZADA_POR_POSTULANTE', 'EXPIRADA', 'ANULADA_POR_COORDINACION'))\nAND (estado_compromiso_anterior IS NULL OR estado_compromiso_anterior IN ('PENDIENTE_CARGA', 'EN_REVISION', 'APROBADO', 'RECHAZADO'))");
                    table.CheckConstraint("ck_historial_estado_beca_estado_nuevo_valido", "(estado_beca_nuevo IS NULL OR estado_beca_nuevo IN ('OTORGADA', 'ACTIVA', 'SUSPENDIDA', 'RENOVADA', 'CONCLUIDA', 'REVOCADA'))\nAND (estado_oferta_nuevo IS NULL OR estado_oferta_nuevo IN ('PENDIENTE_RESPUESTA', 'ACEPTADA', 'RECHAZADA_POR_POSTULANTE', 'EXPIRADA', 'ANULADA_POR_COORDINACION'))\nAND (estado_compromiso_nuevo IS NULL OR estado_compromiso_nuevo IN ('PENDIENTE_CARGA', 'EN_REVISION', 'APROBADO', 'RECHAZADO'))");
                    table.CheckConstraint("ck_historial_estado_beca_tipo_entidad_valido", "tipo_entidad IN ('BECA', 'OFERTA_BECA', 'COMPROMISO_BECARIO')");
                    table.ForeignKey(
                        name: "fk_historial_estado_beca_beca",
                        column: x => x.beca_id,
                        principalSchema: "becas",
                        principalTable: "beca",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "oferta_beca",
                schema: "becas",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    beca_id = table.Column<Guid>(type: "uuid", nullable: false),
                    postulacion_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reserva_presupuesto_id = table.Column<Guid>(type: "uuid", nullable: false),
                    monto_oferta_por_periodo = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    monto_total_estimado_duracion = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    duracion_periodos = table.Column<int>(type: "integer", nullable: false),
                    condiciones_promedio_minimo_renovacion = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    condiciones_materias_minimas_aprobadas = table.Column<int>(type: "integer", nullable: false),
                    condiciones_maximo_materias_reprobadas = table.Column<int>(type: "integer", nullable: false),
                    condiciones_horas_servicio_por_periodo = table.Column<int>(type: "integer", nullable: false),
                    condiciones_consecuencia_incumplimiento = table.Column<string>(type: "varchar", maxLength: 30, nullable: false),
                    url_carta_pdf_firebase = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    hash_carta_sha256 = table.Column<string>(type: "character(64)", fixedLength: true, maxLength: 64, nullable: false),
                    fecha_emision = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    fecha_vencimiento_aceptacion = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    estado = table.Column<string>(type: "varchar", maxLength: 30, nullable: false),
                    motivo_rechazo_o_anulacion = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    fecha_respuesta = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    referencia_job_expiracion_hangfire_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    emitida_por_usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    autorizacion_otp_emision_id = table.Column<Guid>(type: "uuid", nullable: true),
                    autorizacion_otp_aceptacion_id = table.Column<Guid>(type: "uuid", nullable: true),
                    fecha_creacion = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    usuario_creacion_id = table.Column<Guid>(type: "uuid", nullable: false),
                    fecha_ultima_modificacion = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    usuario_modificacion_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_oferta_beca", x => x.id);
                    table.CheckConstraint("ck_oferta_beca_condiciones_validas", "condiciones_promedio_minimo_renovacion >= 0\nAND condiciones_promedio_minimo_renovacion <= 20\nAND condiciones_materias_minimas_aprobadas >= 0\nAND condiciones_maximo_materias_reprobadas >= 0\nAND condiciones_horas_servicio_por_periodo >= 0\nAND condiciones_consecuencia_incumplimiento IN ('LLAMADO_ATENCION', 'SUSPENSION', 'REVOCACION')");
                    table.CheckConstraint("ck_oferta_beca_duracion_valida", "duracion_periodos > 0");
                    table.CheckConstraint("ck_oferta_beca_estado_valido", "estado IN ('PENDIENTE_RESPUESTA', 'ACEPTADA', 'RECHAZADA_POR_POSTULANTE', 'EXPIRADA', 'ANULADA_POR_COORDINACION')");
                    table.CheckConstraint("ck_oferta_beca_fechas_coherentes", "fecha_vencimiento_aceptacion > fecha_emision\nAND (fecha_respuesta IS NULL OR fecha_respuesta >= fecha_emision)");
                    table.CheckConstraint("ck_oferta_beca_montos_no_negativos", "monto_oferta_por_periodo >= 0 AND monto_total_estimado_duracion >= 0");
                    table.ForeignKey(
                        name: "fk_oferta_beca_beca",
                        column: x => x.beca_id,
                        principalSchema: "becas",
                        principalTable: "beca",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "outbox_message",
                schema: "becas",
                columns: table => new
                {
                    SequenceNumber = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EnqueueTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SentTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Headers = table.Column<string>(type: "text", nullable: true),
                    Properties = table.Column<string>(type: "text", nullable: true),
                    InboxMessageId = table.Column<Guid>(type: "uuid", nullable: true),
                    InboxConsumerId = table.Column<Guid>(type: "uuid", nullable: true),
                    OutboxId = table.Column<Guid>(type: "uuid", nullable: true),
                    MessageId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContentType = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    MessageType = table.Column<string>(type: "text", nullable: false),
                    Body = table.Column<string>(type: "text", nullable: false),
                    ConversationId = table.Column<Guid>(type: "uuid", nullable: true),
                    CorrelationId = table.Column<Guid>(type: "uuid", nullable: true),
                    InitiatorId = table.Column<Guid>(type: "uuid", nullable: true),
                    RequestId = table.Column<Guid>(type: "uuid", nullable: true),
                    SourceAddress = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    DestinationAddress = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ResponseAddress = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    FaultAddress = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ExpirationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_outbox_message", x => x.SequenceNumber);
                    table.ForeignKey(
                        name: "FK_outbox_message_inbox_state_InboxMessageId_InboxConsumerId",
                        columns: x => new { x.InboxMessageId, x.InboxConsumerId },
                        principalSchema: "becas",
                        principalTable: "inbox_state",
                        principalColumns: new[] { "MessageId", "ConsumerId" });
                    table.ForeignKey(
                        name: "FK_outbox_message_outbox_state_OutboxId",
                        column: x => x.OutboxId,
                        principalSchema: "becas",
                        principalTable: "outbox_state",
                        principalColumn: "OutboxId");
                });

            migrationBuilder.CreateIndex(
                name: "ux_beca_cadena_periodo",
                schema: "becas",
                table: "beca",
                columns: new[] { "cadena_beca_id", "periodo_vigente_actual_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_beca_codigo",
                schema: "becas",
                table: "beca",
                column: "codigo_beca",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_beca_estudiante_cobertura_total_activa",
                schema: "becas",
                table: "beca",
                column: "postulante_estudiante_id",
                unique: true,
                filter: "estado = 'ACTIVA' AND cobertura_modalidad = 'TOTAL'");

            migrationBuilder.CreateIndex(
                name: "ux_beca_estudiante_periodo_no_acumulable_activa",
                schema: "becas",
                table: "beca",
                columns: new[] { "postulante_estudiante_id", "periodo_vigente_actual_id" },
                unique: true,
                filter: "estado = 'ACTIVA' AND permite_acumulacion = false");

            migrationBuilder.CreateIndex(
                name: "ux_compromiso_becario_beca",
                schema: "becas",
                table: "compromiso_becario",
                column: "beca_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_historial_estado_beca_beca_id",
                schema: "becas",
                table: "historial_estado_beca",
                column: "beca_id");

            migrationBuilder.CreateIndex(
                name: "ux_idempotency_request_endpoint_clave",
                schema: "becas",
                table: "idempotency_request",
                columns: new[] { "endpoint", "clave" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_inbox_state_Delivered",
                schema: "becas",
                table: "inbox_state",
                column: "Delivered");

            migrationBuilder.CreateIndex(
                name: "ux_oferta_beca_beca",
                schema: "becas",
                table: "oferta_beca",
                column: "beca_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_outbox_message_EnqueueTime",
                schema: "becas",
                table: "outbox_message",
                column: "EnqueueTime");

            migrationBuilder.CreateIndex(
                name: "IX_outbox_message_ExpirationTime",
                schema: "becas",
                table: "outbox_message",
                column: "ExpirationTime");

            migrationBuilder.CreateIndex(
                name: "IX_outbox_message_InboxMessageId_InboxConsumerId_SequenceNumber",
                schema: "becas",
                table: "outbox_message",
                columns: new[] { "InboxMessageId", "InboxConsumerId", "SequenceNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_outbox_message_OutboxId_SequenceNumber",
                schema: "becas",
                table: "outbox_message",
                columns: new[] { "OutboxId", "SequenceNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_outbox_state_Created",
                schema: "becas",
                table: "outbox_state",
                column: "Created");

            migrationBuilder.Sql(
                """
                CREATE OR REPLACE FUNCTION becas.fn_historial_estado_beca_inmutable()
                RETURNS trigger
                LANGUAGE plpgsql
                AS $$
                BEGIN
                    RAISE EXCEPTION 'El historial de estados de beca es inmutable: no se permite UPDATE ni DELETE.'
                        USING ERRCODE = 'integrity_constraint_violation';
                END;
                $$;

                CREATE TRIGGER trg_historial_estado_beca_inmutable
                BEFORE UPDATE OR DELETE ON becas.historial_estado_beca
                FOR EACH ROW
                EXECUTE FUNCTION becas.fn_historial_estado_beca_inmutable();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP TRIGGER IF EXISTS trg_historial_estado_beca_inmutable
                    ON becas.historial_estado_beca;
                DROP FUNCTION IF EXISTS becas.fn_historial_estado_beca_inmutable();
                """);

            migrationBuilder.DropTable(
                name: "compromiso_becario",
                schema: "becas");

            migrationBuilder.DropTable(
                name: "convenio_ref",
                schema: "becas");

            migrationBuilder.DropTable(
                name: "costo_matricula_ref",
                schema: "becas");

            migrationBuilder.DropTable(
                name: "historial_estado_beca",
                schema: "becas");

            migrationBuilder.DropTable(
                name: "idempotency_request",
                schema: "becas");

            migrationBuilder.DropTable(
                name: "oferta_beca",
                schema: "becas");

            migrationBuilder.DropTable(
                name: "outbox_message",
                schema: "becas");

            migrationBuilder.DropTable(
                name: "programa_beca_ref",
                schema: "becas");

            migrationBuilder.DropTable(
                name: "beca",
                schema: "becas");

            migrationBuilder.DropTable(
                name: "inbox_state",
                schema: "becas");

            migrationBuilder.DropTable(
                name: "outbox_state",
                schema: "becas");
        }
    }
}
