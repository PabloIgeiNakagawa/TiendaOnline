using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TiendaOnline.Migrations
{
    /// <inheritdoc />
    public partial class AgregueRol : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Rol",
                table: "Usuarios",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Rol",
                table: "Usuarios");
        }
    }
}
