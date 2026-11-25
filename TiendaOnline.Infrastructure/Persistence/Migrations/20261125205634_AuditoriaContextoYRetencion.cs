using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TiendaOnline.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AuditoriaContextoYRetencion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Recorta valores viejos antes de acotar las columnas: el ALTER falla
            // si alguna fila supera el nuevo largo y la app no arrancaría en el deploy.
            migrationBuilder.Sql(
                "UPDATE Auditorias SET Accion = LEFT(Accion, 200) WHERE LEN(Accion) > 200;");
            migrationBuilder.Sql(
                "UPDATE Auditorias SET TablaAfectada = LEFT(TablaAfectada, 100) WHERE LEN(TablaAfectada) > 100;");
            migrationBuilder.Sql(
                "UPDATE Auditorias SET EntidadId = LEFT(EntidadId, 100) WHERE LEN(EntidadId) > 100;");

            migrationBuilder.AlterColumn<string>(
                name: "TablaAfectada",
                table: "Auditorias",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "EntidadId",
                table: "Auditorias",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Accion",
                table: "Auditorias",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<string>(
                name: "CorrelationId",
                table: "Auditorias",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Ip",
                table: "Auditorias",
                type: "nvarchar(45)",
                maxLength: 45,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Origen",
                table: "Auditorias",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UserAgent",
                table: "Auditorias",
                type: "nvarchar(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Auditorias_Fecha",
                table: "Auditorias",
                column: "Fecha");

            migrationBuilder.CreateIndex(
                name: "IX_Auditorias_TablaAfectada_EntidadId",
                table: "Auditorias",
                columns: new[] { "TablaAfectada", "EntidadId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Auditorias_Fecha",
                table: "Auditorias");

            migrationBuilder.DropIndex(
                name: "IX_Auditorias_TablaAfectada_EntidadId",
                table: "Auditorias");

            migrationBuilder.DropColumn(
                name: "CorrelationId",
                table: "Auditorias");

            migrationBuilder.DropColumn(
                name: "Ip",
                table: "Auditorias");

            migrationBuilder.DropColumn(
                name: "Origen",
                table: "Auditorias");

            migrationBuilder.DropColumn(
                name: "UserAgent",
                table: "Auditorias");

            migrationBuilder.AlterColumn<string>(
                name: "TablaAfectada",
                table: "Auditorias",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "EntidadId",
                table: "Auditorias",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "Accion",
                table: "Auditorias",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);
        }
    }
}
