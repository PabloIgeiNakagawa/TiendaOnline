using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TiendaOnline.Data.Migrations
{
    /// <inheritdoc />
    public partial class RelacionPedidoMovimiento : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MovimientosStock_Pedidos_PedidoId",
                table: "MovimientosStock");

            migrationBuilder.AddForeignKey(
                name: "FK_MovimientosStock_Pedidos_PedidoId",
                table: "MovimientosStock",
                column: "PedidoId",
                principalTable: "Pedidos",
                principalColumn: "PedidoId",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MovimientosStock_Pedidos_PedidoId",
                table: "MovimientosStock");

            migrationBuilder.AddForeignKey(
                name: "FK_MovimientosStock_Pedidos_PedidoId",
                table: "MovimientosStock",
                column: "PedidoId",
                principalTable: "Pedidos",
                principalColumn: "PedidoId");
        }
    }
}
