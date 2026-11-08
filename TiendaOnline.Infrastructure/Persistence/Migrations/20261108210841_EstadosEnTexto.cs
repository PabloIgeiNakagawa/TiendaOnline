using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TiendaOnline.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class EstadosEnTexto : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Rol",
                table: "Usuarios",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "EstadoPago",
                table: "Pedidos",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "Estado",
                table: "Pedidos",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            // SQL Server dejó los enteros como sus dígitos ('4') en el ALTER;
            // el renombrado a los nombres del negocio es lo que hace legible la base.
            migrationBuilder.Sql(@"
UPDATE Pedidos SET Estado = CASE Estado
    WHEN '0' THEN N'Nuevo'
    WHEN '1' THEN N'EnPreparacion'
    WHEN '2' THEN N'Enviado'
    WHEN '3' THEN N'Entregado'
    WHEN '4' THEN N'Cancelado' END
UPDATE Pedidos SET EstadoPago = CASE EstadoPago
    WHEN '0' THEN N'Pendiente'
    WHEN '1' THEN N'Aprobado'
    WHEN '2' THEN N'Rechazado'
    WHEN '3' THEN N'Reembolsado' END
UPDATE Usuarios SET Rol = CASE Rol
    WHEN '0' THEN N'Usuario'
    WHEN '1' THEN N'Administrador'
    WHEN '2' THEN N'Repartidor' END");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Orden inverso: el texto vuelve a enteros ANTES del ALTER a int
            // (SQL Server no puede convertir 'EnPreparacion' → int implícitamente).
            migrationBuilder.Sql(@"
UPDATE Pedidos SET Estado = CASE Estado
    WHEN N'Nuevo' THEN '0'
    WHEN N'EnPreparacion' THEN '1'
    WHEN N'Enviado' THEN '2'
    WHEN N'Entregado' THEN '3'
    WHEN N'Cancelado' THEN '4' END
UPDATE Pedidos SET EstadoPago = CASE EstadoPago
    WHEN N'Pendiente' THEN '0'
    WHEN N'Aprobado' THEN '1'
    WHEN N'Rechazado' THEN '2'
    WHEN N'Reembolsado' THEN '3' END
UPDATE Usuarios SET Rol = CASE Rol
    WHEN N'Usuario' THEN '0'
    WHEN N'Administrador' THEN '1'
    WHEN N'Repartidor' THEN '2' END");

            migrationBuilder.AlterColumn<int>(
                name: "Rol",
                table: "Usuarios",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(30)",
                oldMaxLength: 30);

            migrationBuilder.AlterColumn<int>(
                name: "EstadoPago",
                table: "Pedidos",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(30)",
                oldMaxLength: 30);

            migrationBuilder.AlterColumn<int>(
                name: "Estado",
                table: "Pedidos",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(30)",
                oldMaxLength: 30);
        }
    }
}
