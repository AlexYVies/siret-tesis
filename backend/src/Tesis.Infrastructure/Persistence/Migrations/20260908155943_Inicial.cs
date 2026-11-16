using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tesis.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Inicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FechaReg = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaMod = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FechaReg = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaMod = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SecurityStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "bit", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "bit", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TiposArticulo",
                columns: table => new
                {
                    IdTipoArticulo = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Activo = table.Column<bool>(type: "bit", nullable: false),
                    FechaReg = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaMod = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TiposArticulo", x => x.IdTipoArticulo);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleId = table.Column<int>(type: "int", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Alumnos",
                columns: table => new
                {
                    IdAlumno = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdUsuario = table.Column<int>(type: "int", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    NumeroControl = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Correo = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: false),
                    FechaReg = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaMod = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Alumnos", x => x.IdAlumno);
                    table.ForeignKey(
                        name: "FK_Alumnos_AspNetUsers_IdUsuario",
                        column: x => x.IdUsuario,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderKey = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false),
                    RoleId = table.Column<int>(type: "int", nullable: false),
                    FechaReg = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaMod = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UsuarioId = table.Column<int>(type: "int", nullable: true),
                    RolId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RolId",
                        column: x => x.RolId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false),
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Profesores",
                columns: table => new
                {
                    IdProfesor = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdUsuario = table.Column<int>(type: "int", nullable: true),
                    Nombre = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false),
                    FechaReg = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaMod = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Profesores", x => x.IdProfesor);
                    table.ForeignKey(
                        name: "FK_Profesores_AspNetUsers_IdUsuario",
                        column: x => x.IdUsuario,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProfesorPosgrado",
                columns: table => new
                {
                    IdProfesorPosgrado = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdProfesor = table.Column<int>(type: "int", nullable: false),
                    Nivel = table.Column<int>(type: "int", nullable: false),
                    Programa = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    FechaReg = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaMod = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProfesorPosgrado", x => x.IdProfesorPosgrado);
                    table.ForeignKey(
                        name: "FK_ProfesorPosgrado_Profesores_IdProfesor",
                        column: x => x.IdProfesor,
                        principalTable: "Profesores",
                        principalColumn: "IdProfesor",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Tesis",
                columns: table => new
                {
                    Folio = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IdAlumno = table.Column<int>(type: "int", nullable: false),
                    Titulo = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Nivel = table.Column<int>(type: "int", nullable: false),
                    Programa = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    IdAsesor = table.Column<int>(type: "int", nullable: false),
                    TieneCoAsesor = table.Column<bool>(type: "bit", nullable: false),
                    IdCoAsesor = table.Column<int>(type: "int", nullable: true),
                    Version = table.Column<int>(type: "int", nullable: false),
                    Estado = table.Column<int>(type: "int", nullable: false),
                    FechaExamenGrado = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FechaReg = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaMod = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tesis", x => x.Folio);
                    table.ForeignKey(
                        name: "FK_Tesis_Alumnos_IdAlumno",
                        column: x => x.IdAlumno,
                        principalTable: "Alumnos",
                        principalColumn: "IdAlumno",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Tesis_Profesores_IdAsesor",
                        column: x => x.IdAsesor,
                        principalTable: "Profesores",
                        principalColumn: "IdProfesor",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Tesis_Profesores_IdCoAsesor",
                        column: x => x.IdCoAsesor,
                        principalTable: "Profesores",
                        principalColumn: "IdProfesor",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Articulos",
                columns: table => new
                {
                    IdArticulo = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Folio = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Url = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Doi = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IdTipoArticulo = table.Column<int>(type: "int", nullable: true),
                    FechaReg = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaMod = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Articulos", x => x.IdArticulo);
                    table.ForeignKey(
                        name: "FK_Articulos_Tesis_Folio",
                        column: x => x.Folio,
                        principalTable: "Tesis",
                        principalColumn: "Folio",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Articulos_TiposArticulo_IdTipoArticulo",
                        column: x => x.IdTipoArticulo,
                        principalTable: "TiposArticulo",
                        principalColumn: "IdTipoArticulo",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ConfirmacionesAsesoria",
                columns: table => new
                {
                    IdConfirmacion = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Folio = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IdProfesor = table.Column<int>(type: "int", nullable: false),
                    Tipo = table.Column<int>(type: "int", nullable: false),
                    Estado = table.Column<int>(type: "int", nullable: false),
                    Fecha = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaReg = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaMod = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConfirmacionesAsesoria", x => x.IdConfirmacion);
                    table.ForeignKey(
                        name: "FK_ConfirmacionesAsesoria_Profesores_IdProfesor",
                        column: x => x.IdProfesor,
                        principalTable: "Profesores",
                        principalColumn: "IdProfesor",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ConfirmacionesAsesoria_Tesis_Folio",
                        column: x => x.Folio,
                        principalTable: "Tesis",
                        principalColumn: "Folio",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Dictamenes",
                columns: table => new
                {
                    IdDictamen = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Folio = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IdDirector = table.Column<int>(type: "int", nullable: false),
                    Resultado = table.Column<int>(type: "int", nullable: false),
                    Observaciones = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Fecha = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaReg = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaMod = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Dictamenes", x => x.IdDictamen);
                    table.ForeignKey(
                        name: "FK_Dictamenes_Tesis_Folio",
                        column: x => x.Folio,
                        principalTable: "Tesis",
                        principalColumn: "Folio",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DocumentosTesis",
                columns: table => new
                {
                    IdDocumento = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Folio = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    FechaReg = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaMod = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentosTesis", x => x.IdDocumento);
                    table.ForeignKey(
                        name: "FK_DocumentosTesis_Tesis_Folio",
                        column: x => x.Folio,
                        principalTable: "Tesis",
                        principalColumn: "Folio",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Favoritos",
                columns: table => new
                {
                    IdFavorito = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdUsuario = table.Column<int>(type: "int", nullable: false),
                    Folio = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FechaReg = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaMod = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Favoritos", x => x.IdFavorito);
                    table.ForeignKey(
                        name: "FK_Favoritos_AspNetUsers_IdUsuario",
                        column: x => x.IdUsuario,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Favoritos_Tesis_Folio",
                        column: x => x.Folio,
                        principalTable: "Tesis",
                        principalColumn: "Folio",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RevisionesAsesor",
                columns: table => new
                {
                    IdRevisionAsesor = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Folio = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IdProfesor = table.Column<int>(type: "int", nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    EsUltimaVersion = table.Column<bool>(type: "bit", nullable: false),
                    Decision = table.Column<int>(type: "int", nullable: false),
                    Observaciones = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Fecha = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaReg = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaMod = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RevisionesAsesor", x => x.IdRevisionAsesor);
                    table.ForeignKey(
                        name: "FK_RevisionesAsesor_Profesores_IdProfesor",
                        column: x => x.IdProfesor,
                        principalTable: "Profesores",
                        principalColumn: "IdProfesor",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RevisionesAsesor_Tesis_Folio",
                        column: x => x.Folio,
                        principalTable: "Tesis",
                        principalColumn: "Folio",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RevisionesIA",
                columns: table => new
                {
                    IdRevision = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Folio = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    Resultado = table.Column<int>(type: "int", nullable: false),
                    Porcentaje = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    Fecha = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaReg = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaMod = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RevisionesIA", x => x.IdRevision);
                    table.ForeignKey(
                        name: "FK_RevisionesIA_Tesis_Folio",
                        column: x => x.Folio,
                        principalTable: "Tesis",
                        principalColumn: "Folio",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Alumnos_IdUsuario",
                table: "Alumnos",
                column: "IdUsuario",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Articulos_Folio",
                table: "Articulos",
                column: "Folio");

            migrationBuilder.CreateIndex(
                name: "IX_Articulos_IdTipoArticulo",
                table: "Articulos",
                column: "IdTipoArticulo");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true,
                filter: "[NormalizedName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RolId",
                table: "AspNetUserRoles",
                column: "RolId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_UsuarioId",
                table: "AspNetUserRoles",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true,
                filter: "[NormalizedUserName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ConfirmacionesAsesoria_Folio",
                table: "ConfirmacionesAsesoria",
                column: "Folio");

            migrationBuilder.CreateIndex(
                name: "IX_ConfirmacionesAsesoria_IdProfesor",
                table: "ConfirmacionesAsesoria",
                column: "IdProfesor");

            migrationBuilder.CreateIndex(
                name: "IX_Dictamenes_Folio",
                table: "Dictamenes",
                column: "Folio");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentosTesis_Folio",
                table: "DocumentosTesis",
                column: "Folio");

            migrationBuilder.CreateIndex(
                name: "IX_Favoritos_Folio",
                table: "Favoritos",
                column: "Folio");

            migrationBuilder.CreateIndex(
                name: "IX_Favoritos_IdUsuario",
                table: "Favoritos",
                column: "IdUsuario");

            migrationBuilder.CreateIndex(
                name: "IX_Profesores_IdUsuario",
                table: "Profesores",
                column: "IdUsuario",
                unique: true,
                filter: "[IdUsuario] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ProfesorPosgrado_IdProfesor",
                table: "ProfesorPosgrado",
                column: "IdProfesor");

            migrationBuilder.CreateIndex(
                name: "IX_RevisionesAsesor_Folio",
                table: "RevisionesAsesor",
                column: "Folio");

            migrationBuilder.CreateIndex(
                name: "IX_RevisionesAsesor_IdProfesor",
                table: "RevisionesAsesor",
                column: "IdProfesor");

            migrationBuilder.CreateIndex(
                name: "IX_RevisionesIA_Folio",
                table: "RevisionesIA",
                column: "Folio");

            migrationBuilder.CreateIndex(
                name: "IX_Tesis_IdAlumno",
                table: "Tesis",
                column: "IdAlumno");

            migrationBuilder.CreateIndex(
                name: "IX_Tesis_IdAsesor",
                table: "Tesis",
                column: "IdAsesor");

            migrationBuilder.CreateIndex(
                name: "IX_Tesis_IdCoAsesor",
                table: "Tesis",
                column: "IdCoAsesor");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Articulos");

            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "ConfirmacionesAsesoria");

            migrationBuilder.DropTable(
                name: "Dictamenes");

            migrationBuilder.DropTable(
                name: "DocumentosTesis");

            migrationBuilder.DropTable(
                name: "Favoritos");

            migrationBuilder.DropTable(
                name: "ProfesorPosgrado");

            migrationBuilder.DropTable(
                name: "RevisionesAsesor");

            migrationBuilder.DropTable(
                name: "RevisionesIA");

            migrationBuilder.DropTable(
                name: "TiposArticulo");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "Tesis");

            migrationBuilder.DropTable(
                name: "Alumnos");

            migrationBuilder.DropTable(
                name: "Profesores");

            migrationBuilder.DropTable(
                name: "AspNetUsers");
        }
    }
}
