using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TiendaOnline.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MetodoDePagoYCambiosPedido : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EstadoPago",
                table: "Pedidos",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MetodoDePagoId",
                table: "Pedidos",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "TransaccionPagoId",
                table: "Pedidos",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "MetodoDePago",
                columns: table => new
                {
                    MetodoDePagoId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MetodoDePago", x => x.MetodoDePagoId);
                });

            migrationBuilder.InsertData(
                table: "MetodoDePago",
                columns: new[] { "MetodoDePagoId", "Nombre", "Activo" },
                values: new object[,]
                {
                    { 1, "Mercado Pago", true },
                    { 2, "Transferencia Bancaria", true },
                    { 3, "Efectivo", true }
                });

            migrationBuilder.Sql(
                "UPDATE Pedidos SET MetodoDePagoId = 1 WHERE MetodoDePagoId = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Pedidos_MetodoDePagoId",
                table: "Pedidos",
                column: "MetodoDePagoId");



            migrationBuilder.AddForeignKey(
                name: "FK_Pedidos_MetodoDePago_MetodoDePagoId",
                table: "Pedidos",
                column: "MetodoDePagoId",
                principalTable: "MetodoDePago",
                principalColumn: "MetodoDePagoId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Pedidos_MetodoDePago_MetodoDePagoId",
                table: "Pedidos");

            migrationBuilder.DropTable(
                name: "MetodoDePago");

            migrationBuilder.DropIndex(
                name: "IX_Pedidos_MetodoDePagoId",
                table: "Pedidos");

            migrationBuilder.DropColumn(
                name: "EstadoPago",
                table: "Pedidos");

            migrationBuilder.DropColumn(
                name: "MetodoDePagoId",
                table: "Pedidos");

            migrationBuilder.DropColumn(
                name: "TransaccionPagoId",
                table: "Pedidos");
        }
    }
}
