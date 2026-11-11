using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace BlackHellHeim.Infraestructura.Persistencia.Migraciones
{
    /// <inheritdoc />
    public partial class MigracionInicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "dbo");

            migrationBuilder.CreateTable(
                name: "Tiendas",
                schema: "dbo",
                columns: table => new
                {
                    Codigo = table.Column<string>(type: "char(3)", unicode: false, fixedLength: true, maxLength: 3, nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Moneda = table.Column<string>(type: "char(3)", unicode: false, fixedLength: true, maxLength: 3, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tiendas", x => x.Codigo);
                });

            migrationBuilder.InsertData(
                schema: "dbo",
                table: "Tiendas",
                columns: new[] { "Codigo", "Moneda", "Nombre" },
                values: new object[,]
                {
                    { "BHH", "MXN", "BlackHellHeim" },
                    { "DEM", "MXN", "Tienda de demostración" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Tiendas",
                schema: "dbo");
        }
    }
}
