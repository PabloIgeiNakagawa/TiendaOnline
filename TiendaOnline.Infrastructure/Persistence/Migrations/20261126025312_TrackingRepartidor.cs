using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TiendaOnline.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class TrackingRepartidor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RepartidorId",
                table: "Pedidos",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "UbicacionesRepartidor",
                columns: table => new
                {
                    UsuarioId = table.Column<int>(type: "int", nullable: false),
                    Latitud = table.Column<double>(type: "float", nullable: false),
                    Longitud = table.Column<double>(type: "float", nullable: false),
                    PrecisionMetros = table.Column<double>(type: "float", nullable: true),
                    ActualizadoEn = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UbicacionesRepartidor", x => x.UsuarioId);
                    table.ForeignKey(
                        name: "FK_UbicacionesRepartidor_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "UsuarioId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Pedidos_RepartidorId",
                table: "Pedidos",
                column: "RepartidorId");

            migrationBuilder.AddForeignKey(
                name: "FK_Pedidos_Usuarios_RepartidorId",
                table: "Pedidos",
                column: "RepartidorId",
                principalTable: "Usuarios",
                principalColumn: "UsuarioId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Pedidos_Usuarios_RepartidorId",
                table: "Pedidos");

            migrationBuilder.DropTable(
                name: "UbicacionesRepartidor");

            migrationBuilder.DropIndex(
                name: "IX_Pedidos_RepartidorId",
                table: "Pedidos");

            migrationBuilder.DropColumn(
                name: "RepartidorId",
                table: "Pedidos");
        }
    }
}
