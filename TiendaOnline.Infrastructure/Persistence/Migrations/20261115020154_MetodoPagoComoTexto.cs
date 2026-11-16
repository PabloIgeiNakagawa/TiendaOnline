using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TiendaOnline.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MetodoPagoComoTexto : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Pedidos_MetodoDePago_MetodoDePagoId",
                table: "Pedidos");

            migrationBuilder.DropIndex(
                name: "IX_Pedidos_MetodoDePagoId",
                table: "Pedidos");

            migrationBuilder.DropTable(
                name: "MetodoDePago");

            migrationBuilder.AddColumn<string>(
                name: "MetodoPago",
                table: "Pedidos",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            // El enum MetodoPago es la única fuente de verdad (la tabla se retiró
            // con esta migration): el id que quedaba en Pedidos renombra a su
            // texto de negocio (1/2/3 de la semilla de MetodoDePagoYCambiosPedido).
            migrationBuilder.Sql(@"
UPDATE Pedidos SET MetodoPago = CASE MetodoDePagoId
    WHEN 1 THEN N'MercadoPago'
    WHEN 2 THEN N'Transferencia'
    WHEN 3 THEN N'Efectivo'
    ELSE N'MercadoPago' END");

            migrationBuilder.AlterColumn<string>(
                name: "MetodoPago",
                table: "Pedidos",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(30)",
                oldMaxLength: 30,
                oldNullable: true);

            migrationBuilder.DropColumn(
                name: "MetodoDePagoId",
                table: "Pedidos");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MetodoDePagoId",
                table: "Pedidos",
                type: "int",
                nullable: true);

            // Orden inverso: el texto vuelve al id de la semilla ANTES del ALTER
            // a not-null (SQL Server no puede convertir 'MercadoPago' → int).
            migrationBuilder.Sql(@"
UPDATE Pedidos SET MetodoDePagoId = CASE MetodoPago
    WHEN N'MercadoPago' THEN 1
    WHEN N'Transferencia' THEN 2
    WHEN N'Efectivo' THEN 3
    ELSE 1 END");

            migrationBuilder.AlterColumn<int>(
                name: "MetodoDePagoId",
                table: "Pedidos",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.DropColumn(
                name: "MetodoPago",
                table: "Pedidos");

            migrationBuilder.CreateTable(
                name: "MetodoDePago",
                columns: table => new
                {
                    MetodoDePagoId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Activo = table.Column<bool>(type: "bit", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
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
    }
}
