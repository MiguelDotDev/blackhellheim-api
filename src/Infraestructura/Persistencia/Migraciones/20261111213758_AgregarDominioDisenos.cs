using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlackHellHeim.Infraestructura.Persistencia.Migraciones
{
    /// <inheritdoc />
    public partial class AgregarDominioDisenos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ArchivosArte",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Url = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    NombreBlob = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    TipoContenido = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TamanoBytes = table.Column<long>(type: "bigint", nullable: false),
                    Ancho = table.Column<int>(type: "int", nullable: false),
                    Alto = table.Column<int>(type: "int", nullable: false),
                    HashSha256 = table.Column<string>(type: "char(64)", unicode: false, fixedLength: true, maxLength: 64, nullable: false),
                    CreadoUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArchivosArte", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProductosBase",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PrintfulProductoId = table.Column<int>(type: "int", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductosBase", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Disenos",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Slug = table.Column<string>(type: "varchar(120)", unicode: false, maxLength: 120, nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Estado = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    ArchivoArteId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreadoUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ActualizadoUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Disenos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Disenos_ArchivosArte_ArchivoArteId",
                        column: x => x.ArchivoArteId,
                        principalSchema: "dbo",
                        principalTable: "ArchivosArte",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Variantes",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductoBaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PrintfulVarianteId = table.Column<int>(type: "int", nullable: false),
                    Color = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    CodigoColor = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Talla = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CostoBaseUsd = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    Activa = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Variantes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Variantes_ProductosBase_ProductoBaseId",
                        column: x => x.ProductoBaseId,
                        principalSchema: "dbo",
                        principalTable: "ProductosBase",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DisenosVariantes",
                schema: "dbo",
                columns: table => new
                {
                    DisenoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VarianteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DisenosVariantes", x => new { x.DisenoId, x.VarianteId });
                    table.ForeignKey(
                        name: "FK_DisenosVariantes_Disenos_DisenoId",
                        column: x => x.DisenoId,
                        principalSchema: "dbo",
                        principalTable: "Disenos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DisenosVariantes_Variantes_VarianteId",
                        column: x => x.VarianteId,
                        principalSchema: "dbo",
                        principalTable: "Variantes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ArchivosArte_HashSha256",
                schema: "dbo",
                table: "ArchivosArte",
                column: "HashSha256",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Disenos_ArchivoArteId",
                schema: "dbo",
                table: "Disenos",
                column: "ArchivoArteId");

            migrationBuilder.CreateIndex(
                name: "IX_Disenos_Estado_CreadoUtc",
                schema: "dbo",
                table: "Disenos",
                columns: new[] { "Estado", "CreadoUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Disenos_Nombre",
                schema: "dbo",
                table: "Disenos",
                column: "Nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Disenos_Slug",
                schema: "dbo",
                table: "Disenos",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DisenosVariantes_VarianteId",
                schema: "dbo",
                table: "DisenosVariantes",
                column: "VarianteId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductosBase_PrintfulProductoId",
                schema: "dbo",
                table: "ProductosBase",
                column: "PrintfulProductoId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Variantes_PrintfulVarianteId",
                schema: "dbo",
                table: "Variantes",
                column: "PrintfulVarianteId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Variantes_ProductoBaseId",
                schema: "dbo",
                table: "Variantes",
                column: "ProductoBaseId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DisenosVariantes",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Disenos",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Variantes",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "ArchivosArte",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "ProductosBase",
                schema: "dbo");
        }
    }
}
