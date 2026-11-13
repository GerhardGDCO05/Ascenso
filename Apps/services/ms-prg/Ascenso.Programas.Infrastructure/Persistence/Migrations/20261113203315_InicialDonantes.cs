using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Ascenso.Programas.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InicialDonantes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "programas");

            migrationBuilder.CreateTable(
                name: "donantes",
                schema: "programas",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tipo_donante = table.Column<string>(type: "varchar", maxLength: 30, nullable: false),
                    razon_social_nombre = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    identificacion_fiscal = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    telefono = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    direccion_fiscal = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: false),
                    contacto_nombre_representante = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    contacto_cargo = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    contacto_email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    contacto_telefono = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    estado_donante = table.Column<string>(type: "varchar", maxLength: 20, nullable: false),
                    moneda = table.Column<string>(type: "character(3)", fixedLength: true, maxLength: 3, nullable: false),
                    saldo_total_historico = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    saldo_disponible_asignacion = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    fecha_creacion = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    usuario_creacion_id = table.Column<Guid>(type: "uuid", nullable: false),
                    fecha_ultima_modificacion = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    usuario_modificacion_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_donantes", x => x.id);
                    table.CheckConstraint("ck_donante_estado_valido", "estado_donante IN ('ACTIVO', 'INACTIVO')");
                    table.CheckConstraint("ck_donante_saldos_no_negativos", "saldo_total_historico >= 0\nAND saldo_disponible_asignacion >= 0\nAND saldo_disponible_asignacion <= saldo_total_historico");
                    table.CheckConstraint("ck_donante_tipo_valido", "tipo_donante IN ('EMPRESA', 'ORGANIZACION', 'EGRESADO_PERSONA')");
                });

            migrationBuilder.CreateTable(
                name: "inbox_state",
                schema: "programas",
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
                schema: "programas",
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
                name: "aportes_donante",
                schema: "programas",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    donante_id = table.Column<Guid>(type: "uuid", nullable: false),
                    monto_aporte = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    moneda = table.Column<string>(type: "character(3)", fixedLength: true, maxLength: 3, nullable: false),
                    periodo_academico_id = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    fecha_aporte = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    banco_emisor = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    numero_referencia_bancaria = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    url_comprobante_firebase = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    estado_aporte = table.Column<string>(type: "varchar", maxLength: 30, nullable: false),
                    observaciones = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    fecha_creacion = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    usuario_autor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    correlacion_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_aportes_donante", x => x.id);
                    table.CheckConstraint("ck_aporte_donante_estado_valido", "estado_aporte IN ('PENDIENTE_VERIFICACION', 'CONFIRMADO', 'ANULADO')");
                    table.CheckConstraint("ck_aporte_donante_monto_positivo", "monto_aporte > 0");
                    table.ForeignKey(
                        name: "fk_aportes_donante_donante",
                        column: x => x.donante_id,
                        principalSchema: "programas",
                        principalTable: "donantes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "outbox_message",
                schema: "programas",
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
                        principalSchema: "programas",
                        principalTable: "inbox_state",
                        principalColumns: new[] { "MessageId", "ConsumerId" });
                    table.ForeignKey(
                        name: "FK_outbox_message_outbox_state_OutboxId",
                        column: x => x.OutboxId,
                        principalSchema: "programas",
                        principalTable: "outbox_state",
                        principalColumn: "OutboxId");
                });

            migrationBuilder.CreateIndex(
                name: "ix_aporte_donante_donante_periodo",
                schema: "programas",
                table: "aportes_donante",
                columns: new[] { "donante_id", "periodo_academico_id", "fecha_aporte" },
                descending: new[] { false, false, true });

            migrationBuilder.CreateIndex(
                name: "ux_aporte_donante_referencia_bancaria",
                schema: "programas",
                table: "aportes_donante",
                column: "numero_referencia_bancaria",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_donante_identificacion_fiscal",
                schema: "programas",
                table: "donantes",
                column: "identificacion_fiscal",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_inbox_state_Delivered",
                schema: "programas",
                table: "inbox_state",
                column: "Delivered");

            migrationBuilder.CreateIndex(
                name: "IX_outbox_message_EnqueueTime",
                schema: "programas",
                table: "outbox_message",
                column: "EnqueueTime");

            migrationBuilder.CreateIndex(
                name: "IX_outbox_message_ExpirationTime",
                schema: "programas",
                table: "outbox_message",
                column: "ExpirationTime");

            migrationBuilder.CreateIndex(
                name: "IX_outbox_message_InboxMessageId_InboxConsumerId_SequenceNumber",
                schema: "programas",
                table: "outbox_message",
                columns: new[] { "InboxMessageId", "InboxConsumerId", "SequenceNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_outbox_message_OutboxId_SequenceNumber",
                schema: "programas",
                table: "outbox_message",
                columns: new[] { "OutboxId", "SequenceNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_outbox_state_Created",
                schema: "programas",
                table: "outbox_state",
                column: "Created");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "aportes_donante",
                schema: "programas");

            migrationBuilder.DropTable(
                name: "outbox_message",
                schema: "programas");

            migrationBuilder.DropTable(
                name: "donantes",
                schema: "programas");

            migrationBuilder.DropTable(
                name: "inbox_state",
                schema: "programas");

            migrationBuilder.DropTable(
                name: "outbox_state",
                schema: "programas");
        }
    }
}
