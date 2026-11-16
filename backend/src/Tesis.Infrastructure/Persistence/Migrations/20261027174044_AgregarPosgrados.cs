using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tesis.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AgregarPosgrados : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Posgrados",
                columns: table => new
                {
                    IdPosgrado = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nivel = table.Column<int>(type: "int", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false),
                    FechaReg = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaMod = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Posgrados", x => x.IdPosgrado);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Posgrados_Nombre_Nivel",
                table: "Posgrados",
                columns: new[] { "Nombre", "Nivel" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Posgrados");
        }
    }
}
