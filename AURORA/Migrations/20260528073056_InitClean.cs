using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AURORA.Migrations
{
    /// <inheritdoc />
    public partial class InitClean : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Libros",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Titulo = table.Column<string>(type: "text", nullable: true),
                    Autor = table.Column<string>(type: "text", nullable: true),
                    Genero = table.Column<string>(type: "text", nullable: true),
                    Paginas = table.Column<int>(type: "integer", nullable: true),
                    Editorial = table.Column<string>(type: "text", nullable: true),
                    Año = table.Column<int>(type: "integer", nullable: true),
                    RutaPdf = table.Column<string>(type: "text", nullable: true),
                    PortadaUrl = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Libros", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Tb_Usuario",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nombres = table.Column<string>(type: "text", nullable: false),
                    ApellidoPaterno = table.Column<string>(type: "text", nullable: false),
                    ApellidoMaterno = table.Column<string>(type: "text", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
                    Password = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Rol = table.Column<string>(type: "text", nullable: true),
                    FotoUrl = table.Column<string>(type: "text", nullable: true),
                    FechaRegistro = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    ResetToken = table.Column<string>(type: "text", nullable: true),
                    ResetTokenExpiry = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tb_Usuario", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Tb_Racha",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UsuarioId = table.Column<int>(type: "integer", nullable: false),
                    DiasConsecutivos = table.Column<int>(type: "integer", nullable: false),
                    UltimaLectura = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    MetaDias = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tb_Racha", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Tb_Racha_Tb_Usuario_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Tb_Usuario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Tb_UsuarioLibro",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UsuarioId = table.Column<int>(type: "integer", nullable: false),
                    LibroId = table.Column<int>(type: "integer", nullable: false),
                    Progreso = table.Column<int>(type: "integer", nullable: false),
                    UltimaPagina = table.Column<int>(type: "integer", nullable: false),
                    FechaInicio = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    FechaFin = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    UltimoAcceso = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    UltimoInicioLectura = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    TiempoLectura = table.Column<long>(type: "bigint", nullable: true),
                    Posicion = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tb_UsuarioLibro", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Tb_UsuarioLibro_Libros_LibroId",
                        column: x => x.LibroId,
                        principalTable: "Libros",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Tb_UsuarioLibro_Tb_Usuario_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Tb_Usuario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TopLibro",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UsuarioId = table.Column<int>(type: "integer", nullable: false),
                    LibroId = table.Column<int>(type: "integer", nullable: false),
                    Posicion = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TopLibro", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TopLibro_Libros_LibroId",
                        column: x => x.LibroId,
                        principalTable: "Libros",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TopLibro_Tb_Usuario_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Tb_Usuario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Tb_Racha_UsuarioId",
                table: "Tb_Racha",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Tb_UsuarioLibro_LibroId",
                table: "Tb_UsuarioLibro",
                column: "LibroId");

            migrationBuilder.CreateIndex(
                name: "IX_Tb_UsuarioLibro_UsuarioId",
                table: "Tb_UsuarioLibro",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_TopLibro_LibroId",
                table: "TopLibro",
                column: "LibroId");

            migrationBuilder.CreateIndex(
                name: "IX_TopLibro_UsuarioId",
                table: "TopLibro",
                column: "UsuarioId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Tb_Racha");

            migrationBuilder.DropTable(
                name: "Tb_UsuarioLibro");

            migrationBuilder.DropTable(
                name: "TopLibro");

            migrationBuilder.DropTable(
                name: "Libros");

            migrationBuilder.DropTable(
                name: "Tb_Usuario");
        }
    }
}
