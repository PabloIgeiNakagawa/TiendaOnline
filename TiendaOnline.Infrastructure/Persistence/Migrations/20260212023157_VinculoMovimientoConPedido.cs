using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TiendaOnline.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class VinculoMovimientoConPedido : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_MovimientosStock_PedidoId",
                table: "MovimientosStock",
                column: "PedidoId");

            migrationBuilder.AddForeignKey(
                name: "FK_MovimientosStock_Pedidos_PedidoId",
                table: "MovimientosStock",
                column: "PedidoId",
                principalTable: "Pedidos",
                principalColumn: "PedidoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MovimientosStock_Pedidos_PedidoId",
                table: "MovimientosStock");

            migrationBuilder.DropIndex(
                name: "IX_MovimientosStock_PedidoId",
                table: "MovimientosStock");
        }
    }
}
