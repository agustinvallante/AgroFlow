using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgroFlow.Data.Migrations.Appointments
{
    /// <inheritdoc />
    public partial class AddLocalDemoTeamAssignments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LocalDemoTeamAssignments",
                columns: table => new
                {
                    SeedId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    TransportistaId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Fingerprint = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    AppliedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LocalDemoTeamAssignments", x => x.SeedId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LocalDemoTeamAssignments_TransportistaId",
                table: "LocalDemoTeamAssignments",
                column: "TransportistaId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LocalDemoTeamAssignments");
        }
    }
}
