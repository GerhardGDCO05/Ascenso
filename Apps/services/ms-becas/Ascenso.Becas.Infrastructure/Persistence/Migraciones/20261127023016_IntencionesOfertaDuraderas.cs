using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ascenso.Becas.Infrastructure.Persistence.Migraciones
{
    /// <inheritdoc />
    public partial class IntencionesOfertaDuraderas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "intencion_aceptacion_oferta",
                schema: "becas",
                columns: table => new
                {
                    oferta_id = table.Column<Guid>(type: "uuid", nullable: false),
                    beca_id = table.Column<Guid>(type: "uuid", nullable: false),
                    payload = table.Column<string>(type: "jsonb", nullable: false),
                    creado_en = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_intencion_aceptacion_oferta", x => x.oferta_id);
                });

            migrationBuilder.CreateTable(
                name: "intencion_emision_oferta",
                schema: "becas",
                columns: table => new
                {
                    oferta_id = table.Column<Guid>(type: "uuid", nullable: false),
                    postulacion_id = table.Column<Guid>(type: "uuid", nullable: false),
                    payload = table.Column<string>(type: "jsonb", nullable: false),
                    creado_en = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_intencion_emision_oferta", x => x.oferta_id);
                });

            migrationBuilder.CreateIndex(
                name: "ux_intencion_aceptacion_oferta_beca",
                schema: "becas",
                table: "intencion_aceptacion_oferta",
                column: "beca_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_intencion_emision_oferta_postulacion",
                schema: "becas",
                table: "intencion_emision_oferta",
                column: "postulacion_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "intencion_aceptacion_oferta",
                schema: "becas");

            migrationBuilder.DropTable(
                name: "intencion_emision_oferta",
                schema: "becas");
        }
    }
}
