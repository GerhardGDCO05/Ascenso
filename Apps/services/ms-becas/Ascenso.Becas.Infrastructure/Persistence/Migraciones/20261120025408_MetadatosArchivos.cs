using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ascenso.Becas.Infrastructure.Persistence.Migraciones
{
    /// <inheritdoc />
    public partial class MetadatosArchivos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "tamanio_carta_pdf_bytes",
                schema: "becas",
                table: "oferta_beca",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<string>(
                name: "tipo_mime_carta_pdf",
                schema: "becas",
                table: "oferta_beca",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<long>(
                name: "tamanio_documento_bytes",
                schema: "becas",
                table: "compromiso_becario",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "tipo_mime_documento",
                schema: "becas",
                table: "compromiso_becario",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_oferta_beca_carta_metadatos_validos",
                schema: "becas",
                table: "oferta_beca",
                sql: "tamanio_carta_pdf_bytes >= 0 AND length(tipo_mime_carta_pdf) > 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_compromiso_becario_tamanio_valido",
                schema: "becas",
                table: "compromiso_becario",
                sql: "tamanio_documento_bytes IS NULL OR tamanio_documento_bytes > 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_oferta_beca_carta_metadatos_validos",
                schema: "becas",
                table: "oferta_beca");

            migrationBuilder.DropCheckConstraint(
                name: "ck_compromiso_becario_tamanio_valido",
                schema: "becas",
                table: "compromiso_becario");

            migrationBuilder.DropColumn(
                name: "tamanio_carta_pdf_bytes",
                schema: "becas",
                table: "oferta_beca");

            migrationBuilder.DropColumn(
                name: "tipo_mime_carta_pdf",
                schema: "becas",
                table: "oferta_beca");

            migrationBuilder.DropColumn(
                name: "tamanio_documento_bytes",
                schema: "becas",
                table: "compromiso_becario");

            migrationBuilder.DropColumn(
                name: "tipo_mime_documento",
                schema: "becas",
                table: "compromiso_becario");
        }
    }
}
