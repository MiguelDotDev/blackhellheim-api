using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlackHellHeim.Infraestructura.Persistencia.Migraciones
{
    /// <inheritdoc />
    public partial class AgregarMockups : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TareasMockup",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DisenoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PrintfulTaskId = table.Column<string>(type: "varchar(60)", unicode: false, maxLength: 60, nullable: false),
                    Estado = table.Column<string>(type: "varchar(12)", unicode: false, maxLength: 12, nullable: false),
                    Motivo = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    CreadaUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    FinalizadaUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TareasMockup", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TareasMockup_Disenos_DisenoId",
                        column: x => x.DisenoId,
                        principalSchema: "dbo",
                        principalTable: "Disenos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ImagenesMockup",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TareaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Color = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    Posicion = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Url = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    NombreBlob = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImagenesMockup", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ImagenesMockup_TareasMockup_TareaId",
                        column: x => x.TareaId,
                        principalSchema: "dbo",
                        principalTable: "TareasMockup",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ImagenesMockup_TareaId_Color_Posicion",
                schema: "dbo",
                table: "ImagenesMockup",
                columns: new[] { "TareaId", "Color", "Posicion" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TareasMockup_Estado_CreadaUtc",
                schema: "dbo",
                table: "TareasMockup",
                columns: new[] { "Estado", "CreadaUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_TareasMockup_PrintfulTaskId",
                schema: "dbo",
                table: "TareasMockup",
                column: "PrintfulTaskId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_TareasMockup_DisenoId_Pendiente",
                schema: "dbo",
                table: "TareasMockup",
                column: "DisenoId",
                unique: true,
                filter: "[Estado] = 'Pendiente'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ImagenesMockup",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "TareasMockup",
                schema: "dbo");
        }
    }
}
