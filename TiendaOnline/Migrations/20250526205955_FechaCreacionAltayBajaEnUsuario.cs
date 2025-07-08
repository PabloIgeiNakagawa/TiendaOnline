using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TiendaOnline.Migrations
{
    /// <inheritdoc />
    public partial class FechaCreacionAltayBajaEnUsuario : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "FechaCreacion",
                table: "Usuarios",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "UltimaFechaAlta",
                table: "Usuarios",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UltimaFechaBaja",
                table: "Usuarios",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FechaCreacion",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "UltimaFechaAlta",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "UltimaFechaBaja",
                table: "Usuarios");
        }
    }
}
