using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tesis.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AgregarHistorialYVersiones : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Favoritos_IdUsuario",
                table: "Favoritos");

            migrationBuilder.Sql("IF COL_LENGTH('Articulos', 'Issn') IS NULL ALTER TABLE [Articulos] ADD [Issn] nvarchar(50) NULL;");

            migrationBuilder.CreateTable(
                name: "HistorialEstadoTesis",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Folio = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EstadoAnterior = table.Column<int>(type: "int", nullable: false),
                    EstadoNuevo = table.Column<int>(type: "int", nullable: false),
                    IdUsuario = table.Column<int>(type: "int", nullable: true),
                    Fecha = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Observacion = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HistorialEstadoTesis", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HistorialEstadoTesis_AspNetUsers_IdUsuario",
                        column: x => x.IdUsuario,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_HistorialEstadoTesis_Tesis_Folio",
                        column: x => x.Folio,
                        principalTable: "Tesis",
                        principalColumn: "Folio",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TesisVersiones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Folio = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    RutaDocumento = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Fecha = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IdUsuario = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TesisVersiones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TesisVersiones_AspNetUsers_IdUsuario",
                        column: x => x.IdUsuario,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_TesisVersiones_Tesis_Folio",
                        column: x => x.Folio,
                        principalTable: "Tesis",
                        principalColumn: "Folio",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Favoritos_IdUsuario_Folio",
                table: "Favoritos",
                columns: new[] { "IdUsuario", "Folio" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HistorialEstadoTesis_Folio",
                table: "HistorialEstadoTesis",
                column: "Folio");

            migrationBuilder.CreateIndex(
                name: "IX_HistorialEstadoTesis_IdUsuario",
                table: "HistorialEstadoTesis",
                column: "IdUsuario");

            migrationBuilder.CreateIndex(
                name: "IX_TesisVersiones_Folio",
                table: "TesisVersiones",
                column: "Folio");

            migrationBuilder.CreateIndex(
                name: "IX_TesisVersiones_IdUsuario",
                table: "TesisVersiones",
                column: "IdUsuario");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HistorialEstadoTesis");

            migrationBuilder.DropTable(
                name: "TesisVersiones");

            migrationBuilder.DropIndex(
                name: "IX_Favoritos_IdUsuario_Folio",
                table: "Favoritos");

            migrationBuilder.DropColumn(
                name: "Issn",
                table: "Articulos");

            migrationBuilder.CreateIndex(
                name: "IX_Favoritos_IdUsuario",
                table: "Favoritos",
                column: "IdUsuario");
        }
    }
}
