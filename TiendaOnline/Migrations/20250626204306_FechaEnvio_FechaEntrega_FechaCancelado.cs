using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TiendaOnline.Migrations
{
    /// <inheritdoc />
    public partial class FechaEnvio_FechaEntrega_FechaCancelado : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "FechaCancelado",
                table: "Pedidos",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaEnvio",
                table: "Pedidos",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FechaCancelado",
                table: "Pedidos");

            migrationBuilder.DropColumn(
                name: "FechaEnvio",
                table: "Pedidos");
        }
    }
}
