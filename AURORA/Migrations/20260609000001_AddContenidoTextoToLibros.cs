using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AURORA.Migrations
{
    /// <inheritdoc />
    public partial class AddContenidoTextoToLibros : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ContenidoTexto",
                table: "Libros",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ContenidoTexto",
                table: "Libros");
        }
    }
}
