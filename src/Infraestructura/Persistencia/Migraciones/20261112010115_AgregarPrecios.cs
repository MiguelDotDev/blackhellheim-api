using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlackHellHeim.Infraestructura.Persistencia.Migraciones
{
    /// <inheritdoc />
    public partial class AgregarPrecios : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ParametrosPrecio",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ColchonCambiario = table.Column<decimal>(type: "decimal(5,4)", precision: 5, scale: 4, nullable: false),
                    Margen = table.Column<decimal>(type: "decimal(5,4)", precision: 5, scale: 4, nullable: false),
                    ComisionPasarela = table.Column<decimal>(type: "decimal(5,4)", precision: 5, scale: 4, nullable: false),
                    ImpresionExtraUsd = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    VigenteDesdeUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ActualizadoPor = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParametrosPrecio", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TiposCambioDiarios",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Fecha = table.Column<DateOnly>(type: "date", nullable: false),
                    Valor = table.Column<decimal>(type: "decimal(10,4)", precision: 10, scale: 4, nullable: false),
                    Fuente = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    ObtenidoUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TiposCambioDiarios", x => x.Id);
                });

            migrationBuilder.InsertData(
                schema: "dbo",
                table: "ParametrosPrecio",
                columns: new[] { "Id", "ActualizadoPor", "ColchonCambiario", "ComisionPasarela", "ImpresionExtraUsd", "Margen", "VigenteDesdeUtc" },
                values: new object[] { new Guid("0b7f3c1e-5a0d-4a53-9c1e-7d0f6b2a4e11"), "semilla", 0.03m, 0.036m, 0m, 0.40m, new DateTimeOffset(new DateTime(2026, 11, 11, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.CreateIndex(
                name: "IX_ParametrosPrecio_VigenteDesdeUtc",
                schema: "dbo",
                table: "ParametrosPrecio",
                column: "VigenteDesdeUtc");

            migrationBuilder.CreateIndex(
                name: "IX_TiposCambioDiarios_Fecha",
                schema: "dbo",
                table: "TiposCambioDiarios",
                column: "Fecha",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ParametrosPrecio",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "TiposCambioDiarios",
                schema: "dbo");
        }
    }
}
