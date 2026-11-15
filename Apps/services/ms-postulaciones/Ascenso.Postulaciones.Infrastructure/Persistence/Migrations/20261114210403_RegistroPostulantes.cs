using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ascenso.Postulaciones.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class RegistroPostulantes : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(
            name: "postulaciones");

        migrationBuilder.CreateTable(
            name: "postulantes",
            schema: "postulaciones",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                persona_id = table.Column<Guid>(type: "uuid", nullable: true),
                usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                numero_documento_identidad = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                nombres = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                apellidos = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                correo_electronico = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                telefono = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                universidad_aliada_id = table.Column<Guid>(type: "uuid", nullable: false),
                carrera_id = table.Column<Guid>(type: "uuid", nullable: false),
                periodo_actual_id = table.Column<Guid>(type: "uuid", nullable: false),
                condicion_academica = table.Column<string>(type: "varchar", maxLength: 20, nullable: false),
                acepta_tratamiento_datos = table.Column<bool>(type: "boolean", nullable: false),
                estado_perfil = table.Column<string>(type: "varchar", maxLength: 20, nullable: false),
                fecha_registro = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                fecha_creacion = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                usuario_creacion_id = table.Column<Guid>(type: "uuid", nullable: false),
                fecha_ultima_modificacion = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                usuario_modificacion_id = table.Column<Guid>(type: "uuid", nullable: false),
                version = table.Column<int>(type: "integer", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_postulantes", x => x.id);
                table.CheckConstraint("ck_postulante_acepta_tratamiento_datos", "acepta_tratamiento_datos = true");
                table.CheckConstraint("ck_postulante_condicion_academica_valida", "condicion_academica IN ('ADMITIDO', 'MATRICULADO')");
                table.CheckConstraint("ck_postulante_estado_perfil_valido", "estado_perfil IN ('INCOMPLETO', 'COMPLETO')");
                table.CheckConstraint("ck_postulante_longitudes_minimas", "char_length(trim(numero_documento_identidad)) >= 5\nAND char_length(trim(nombres)) >= 2\nAND char_length(trim(apellidos)) >= 2");
            });

        migrationBuilder.CreateIndex(
            name: "ux_postulante_numero_documento_identidad",
            schema: "postulaciones",
            table: "postulantes",
            column: "numero_documento_identidad",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ux_postulante_usuario_id",
            schema: "postulaciones",
            table: "postulantes",
            column: "usuario_id",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "postulantes",
            schema: "postulaciones");
    }
}
