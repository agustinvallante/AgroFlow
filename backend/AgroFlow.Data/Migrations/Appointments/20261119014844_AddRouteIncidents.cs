using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgroFlow.Data.Migrations.Appointments
{
    /// <inheritdoc />
    public partial class AddRouteIncidents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "IncidentesRuta",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    IngenioId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TurnoId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Type = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    CanContinue = table.Column<bool>(type: "INTEGER", nullable: false),
                    EstimatedDelayMinutes = table.Column<int>(type: "INTEGER", nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    ReportedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ResolvedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IncidentesRuta", x => x.Id);
                    table.CheckConstraint("CK_IncidentesRuta_EstimatedDelayMinutes_Range", "EstimatedDelayMinutes >= 0 AND EstimatedDelayMinutes <= 1440");
                    table.ForeignKey(
                        name: "FK_IncidentesRuta_Turnos_TurnoId",
                        column: x => x.TurnoId,
                        principalTable: "Turnos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_IncidentesRuta_TurnoId",
                table: "IncidentesRuta",
                column: "TurnoId",
                unique: true,
                filter: "ResolvedAtUtc IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IncidentesRuta");
        }
    }
}
