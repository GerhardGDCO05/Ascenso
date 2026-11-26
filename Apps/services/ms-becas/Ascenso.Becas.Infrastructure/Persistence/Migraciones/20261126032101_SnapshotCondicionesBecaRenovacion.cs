using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ascenso.Becas.Infrastructure.Persistence.Migraciones
{
    /// <inheritdoc />
    public partial class SnapshotCondicionesBecaRenovacion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "condiciones_snapshot_consecuencia_incumplimiento",
                schema: "becas",
                table: "beca",
                type: "varchar",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "condiciones_snapshot_horas_servicio_por_periodo",
                schema: "becas",
                table: "beca",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "condiciones_snapshot_materias_minimas_aprobadas",
                schema: "becas",
                table: "beca",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "condiciones_snapshot_maximo_materias_reprobadas",
                schema: "becas",
                table: "beca",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "condiciones_snapshot_promedio_minimo_renovacion",
                schema: "becas",
                table: "beca",
                type: "numeric(4,2)",
                precision: 4,
                scale: 2,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "condiciones_snapshot_consecuencia_incumplimiento",
                schema: "becas",
                table: "beca");

            migrationBuilder.DropColumn(
                name: "condiciones_snapshot_horas_servicio_por_periodo",
                schema: "becas",
                table: "beca");

            migrationBuilder.DropColumn(
                name: "condiciones_snapshot_materias_minimas_aprobadas",
                schema: "becas",
                table: "beca");

            migrationBuilder.DropColumn(
                name: "condiciones_snapshot_maximo_materias_reprobadas",
                schema: "becas",
                table: "beca");

            migrationBuilder.DropColumn(
                name: "condiciones_snapshot_promedio_minimo_renovacion",
                schema: "becas",
                table: "beca");
        }
    }
}
