using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlackHellHeim.Infraestructura.Persistencia.Migraciones
{
    /// <inheritdoc />
    public partial class AgregarEventosWebhook : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EventosWebhook",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Proveedor = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    IdExterno = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Tipo = table.Column<string>(type: "varchar(80)", unicode: false, maxLength: 80, nullable: false),
                    Estado = table.Column<string>(type: "varchar(12)", unicode: false, maxLength: 12, nullable: false),
                    Cuerpo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RecibidoUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ProcesadoUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Error = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventosWebhook", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EventosWebhook_Estado_RecibidoUtc",
                schema: "dbo",
                table: "EventosWebhook",
                columns: new[] { "Estado", "RecibidoUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_EventosWebhook_Proveedor_IdExterno",
                schema: "dbo",
                table: "EventosWebhook",
                columns: new[] { "Proveedor", "IdExterno" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EventosWebhook",
                schema: "dbo");
        }
    }
}
