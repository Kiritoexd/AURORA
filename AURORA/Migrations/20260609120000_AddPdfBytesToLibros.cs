using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AURORA.Migrations
{
    /// <inheritdoc />
    public partial class AddPdfBytesToLibros : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "PdfBytes",
                table: "Libros",
                type: "bytea",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PdfBytes",
                table: "Libros");
        }
    }
}
