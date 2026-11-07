using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TiendaOnline.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CarritoUsuarioEnBaseDeDatos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CarritosUsuarios",
                columns: table => new
                {
                    UsuarioId = table.Column<int>(type: "int", nullable: false),
                    Contenido = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FechaActualizacion = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CarritosUsuarios", x => x.UsuarioId);
                    table.ForeignKey(
                        name: "FK_CarritosUsuarios_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "UsuarioId",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CarritosUsuarios");
        }
    }
}
