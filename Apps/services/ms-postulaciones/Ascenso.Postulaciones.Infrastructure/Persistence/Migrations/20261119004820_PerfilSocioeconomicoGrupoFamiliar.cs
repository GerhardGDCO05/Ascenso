using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ascenso.Postulaciones.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class PerfilSocioeconomicoGrupoFamiliar : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "estado_civil",
            schema: "postulaciones",
            table: "postulantes",
            type: "varchar",
            maxLength: 20,
            nullable: true);

        migrationBuilder.AddColumn<decimal>(
            name: "ingreso_familiar_mensual_total",
            schema: "postulaciones",
            table: "postulantes",
            type: "numeric(18,2)",
            precision: 18,
            scale: 2,
            nullable: true);

        migrationBuilder.AddColumn<decimal>(
            name: "ingreso_mensual_postulante",
            schema: "postulaciones",
            table: "postulantes",
            type: "numeric(18,2)",
            precision: 18,
            scale: 2,
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "numero_integrantes_familia",
            schema: "postulaciones",
            table: "postulantes",
            type: "integer",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "ocupacion",
            schema: "postulaciones",
            table: "postulantes",
            type: "varchar",
            maxLength: 30,
            nullable: true);

        migrationBuilder.AddColumn<decimal>(
            name: "promedio_actual",
            schema: "postulaciones",
            table: "postulantes",
            type: "numeric(5,2)",
            precision: 5,
            scale: 2,
            nullable: true);

        migrationBuilder.AddColumn<bool>(
            name: "trabaja",
            schema: "postulaciones",
            table: "postulantes",
            type: "boolean",
            nullable: true);

        migrationBuilder.CreateTable(
            name: "miembros_familiares",
            schema: "postulaciones",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                postulante_id = table.Column<Guid>(type: "uuid", nullable: false),
                parentesco = table.Column<string>(type: "varchar", maxLength: 20, nullable: false),
                edad = table.Column<int>(type: "integer", nullable: false),
                ocupacion = table.Column<string>(type: "varchar", maxLength: 30, nullable: false),
                ingreso_mensual = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                fecha_creacion = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                usuario_creacion_id = table.Column<Guid>(type: "uuid", nullable: false),
                fecha_ultima_modificacion = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                usuario_modificacion_id = table.Column<Guid>(type: "uuid", nullable: false),
                version = table.Column<int>(type: "integer", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_miembros_familiares", x => x.id);
                table.CheckConstraint("ck_miembro_familiar_edad_rango", "edad >= 0 AND edad <= 120");
                table.CheckConstraint("ck_miembro_familiar_ingreso_no_negativo", "ingreso_mensual >= 0");
                table.CheckConstraint("ck_miembro_familiar_ocupacion_valida", "ocupacion IN ('EMPLEADO', 'INDEPENDIENTE', 'ESTUDIANTE', 'LABORES_HOGAR', 'JUBILADO', 'NO_TRABAJA', 'OTRO')");
                table.CheckConstraint("ck_miembro_familiar_parentesco_valido", "parentesco IN ('PADRE', 'MADRE', 'HERMANO', 'HIJO', 'PAREJA', 'OTRO')");
                table.ForeignKey(
                    name: "fk_miembros_familiares_postulantes",
                    column: x => x.postulante_id,
                    principalSchema: "postulaciones",
                    principalTable: "postulantes",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.AddCheckConstraint(
            name: "ck_postulante_estado_civil_valido",
            schema: "postulaciones",
            table: "postulantes",
            sql: "estado_civil IS NULL OR estado_civil IN ('SOLTERO', 'CASADO', 'DIVORCIADO', 'VIUDO')");

        migrationBuilder.AddCheckConstraint(
            name: "ck_postulante_ingreso_familiar_total_no_negativo",
            schema: "postulaciones",
            table: "postulantes",
            sql: "ingreso_familiar_mensual_total IS NULL OR ingreso_familiar_mensual_total >= 0");

        migrationBuilder.AddCheckConstraint(
            name: "ck_postulante_ingreso_mensual_no_negativo",
            schema: "postulaciones",
            table: "postulantes",
            sql: "ingreso_mensual_postulante IS NULL OR ingreso_mensual_postulante >= 0");

        migrationBuilder.AddCheckConstraint(
            name: "ck_postulante_numero_integrantes_familia",
            schema: "postulaciones",
            table: "postulantes",
            sql: "numero_integrantes_familia IS NULL OR numero_integrantes_familia >= 1");

        migrationBuilder.AddCheckConstraint(
            name: "ck_postulante_ocupacion_valida",
            schema: "postulaciones",
            table: "postulantes",
            sql: "ocupacion IS NULL OR ocupacion IN ('EMPLEADO', 'INDEPENDIENTE', 'ESTUDIANTE', 'LABORES_HOGAR', 'JUBILADO', 'NO_TRABAJA', 'OTRO')");

        migrationBuilder.AddCheckConstraint(
            name: "ck_postulante_perfil_completo_campos_requeridos",
            schema: "postulaciones",
            table: "postulantes",
            sql: "estado_perfil <> 'COMPLETO' OR (\r\n    estado_civil IS NOT NULL\r\n    AND promedio_actual IS NOT NULL\r\n    AND trabaja IS NOT NULL\r\n    AND ocupacion IS NOT NULL\r\n    AND ingreso_mensual_postulante IS NOT NULL\r\n    AND numero_integrantes_familia IS NOT NULL\r\n    AND ingreso_familiar_mensual_total IS NOT NULL\r\n)");

        migrationBuilder.AddCheckConstraint(
            name: "ck_postulante_promedio_actual_rango",
            schema: "postulaciones",
            table: "postulantes",
            sql: "promedio_actual IS NULL OR (promedio_actual >= 0 AND promedio_actual <= 20)");

        migrationBuilder.CreateIndex(
            name: "ix_miembros_familiares_postulante_id",
            schema: "postulaciones",
            table: "miembros_familiares",
            column: "postulante_id");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "miembros_familiares",
            schema: "postulaciones");

        migrationBuilder.DropCheckConstraint(
            name: "ck_postulante_estado_civil_valido",
            schema: "postulaciones",
            table: "postulantes");

        migrationBuilder.DropCheckConstraint(
            name: "ck_postulante_ingreso_familiar_total_no_negativo",
            schema: "postulaciones",
            table: "postulantes");

        migrationBuilder.DropCheckConstraint(
            name: "ck_postulante_ingreso_mensual_no_negativo",
            schema: "postulaciones",
            table: "postulantes");

        migrationBuilder.DropCheckConstraint(
            name: "ck_postulante_numero_integrantes_familia",
            schema: "postulaciones",
            table: "postulantes");

        migrationBuilder.DropCheckConstraint(
            name: "ck_postulante_ocupacion_valida",
            schema: "postulaciones",
            table: "postulantes");

        migrationBuilder.DropCheckConstraint(
            name: "ck_postulante_perfil_completo_campos_requeridos",
            schema: "postulaciones",
            table: "postulantes");

        migrationBuilder.DropCheckConstraint(
            name: "ck_postulante_promedio_actual_rango",
            schema: "postulaciones",
            table: "postulantes");

        migrationBuilder.DropColumn(
            name: "estado_civil",
            schema: "postulaciones",
            table: "postulantes");

        migrationBuilder.DropColumn(
            name: "ingreso_familiar_mensual_total",
            schema: "postulaciones",
            table: "postulantes");

        migrationBuilder.DropColumn(
            name: "ingreso_mensual_postulante",
            schema: "postulaciones",
            table: "postulantes");

        migrationBuilder.DropColumn(
            name: "numero_integrantes_familia",
            schema: "postulaciones",
            table: "postulantes");

        migrationBuilder.DropColumn(
            name: "ocupacion",
            schema: "postulaciones",
            table: "postulantes");

        migrationBuilder.DropColumn(
            name: "promedio_actual",
            schema: "postulaciones",
            table: "postulantes");

        migrationBuilder.DropColumn(
            name: "trabaja",
            schema: "postulaciones",
            table: "postulantes");
    }
}

