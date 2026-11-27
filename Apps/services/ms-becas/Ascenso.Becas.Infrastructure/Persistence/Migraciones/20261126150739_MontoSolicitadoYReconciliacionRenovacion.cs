using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ascenso.Becas.Infrastructure.Persistence.Migraciones
{
    /// <inheritdoc />
    public partial class MontoSolicitadoYReconciliacionRenovacion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Las solicitudes anteriores no guardaban el monto original.
            // Cero impide aprobarlas sin una nueva solicitud verificable.
            migrationBuilder.AddColumn<decimal>(
                name: "monto_solicitado",
                schema: "becas",
                table: "solicitud_renovacion",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateIndex(
                name: "ix_solicitud_renovacion_estado_fecha_limite",
                schema: "becas",
                table: "solicitud_renovacion",
                columns: new[] { "estado", "fecha_limite_ventana_renovacion" });

            migrationBuilder.AddCheckConstraint(
                name: "ck_solicitud_renovacion_monto_solicitado",
                schema: "becas",
                table: "solicitud_renovacion",
                sql: "monto_solicitado >= 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_solicitud_renovacion_estado_fecha_limite",
                schema: "becas",
                table: "solicitud_renovacion");

            migrationBuilder.DropCheckConstraint(
                name: "ck_solicitud_renovacion_monto_solicitado",
                schema: "becas",
                table: "solicitud_renovacion");

            migrationBuilder.DropColumn(
                name: "monto_solicitado",
                schema: "becas",
                table: "solicitud_renovacion");
        }
    }
}
