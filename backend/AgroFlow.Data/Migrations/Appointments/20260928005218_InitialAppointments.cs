using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgroFlow.Data.Migrations.Appointments
{
    /// <inheritdoc />
    public partial class InitialAppointments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Camiones",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    IngenioId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Plate = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    NormalizedPlate = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    FleetType = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Camiones", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Fincas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    IngenioId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Code = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    NormalizedCode = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    LocationReference = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Fincas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Ingenios",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    TimeZoneId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ingenios", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TransportistaCamiones",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    IngenioId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TransportistaId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CamionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransportistaCamiones", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Transportistas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    IngenioId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    DNI = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    NormalizedDni = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    WhatsApp = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    NormalizedWhatsApp = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Transportistas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Turnos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    IngenioId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TransportistaId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CamionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    FincaId = table.Column<Guid>(type: "TEXT", nullable: false),
                    VentanaId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CutAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    EstimatedLoadTons = table.Column<double>(type: "REAL", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Turnos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Ventanas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    IngenioId = table.Column<Guid>(type: "TEXT", nullable: false),
                    StartAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    EndAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    Capacity = table.Column<int>(type: "INTEGER", nullable: false),
                    Occupied = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ventanas", x => x.Id);
                    table.CheckConstraint("CK_Ventanas_Occupied_Range", "Occupied >= 0 AND Occupied <= Capacity");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Camiones_IngenioId_NormalizedPlate",
                table: "Camiones",
                columns: new[] { "IngenioId", "NormalizedPlate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Fincas_IngenioId_NormalizedCode",
                table: "Fincas",
                columns: new[] { "IngenioId", "NormalizedCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TransportistaCamiones_TransportistaId_CamionId",
                table: "TransportistaCamiones",
                columns: new[] { "TransportistaId", "CamionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Transportistas_IngenioId_NormalizedWhatsApp",
                table: "Transportistas",
                columns: new[] { "IngenioId", "NormalizedWhatsApp" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Turnos_CamionId",
                table: "Turnos",
                column: "CamionId",
                unique: true,
                filter: "IsActive = 1");

            migrationBuilder.CreateIndex(
                name: "IX_Turnos_IngenioId_VentanaId",
                table: "Turnos",
                columns: new[] { "IngenioId", "VentanaId" });

            migrationBuilder.CreateIndex(
                name: "IX_Ventanas_IngenioId_StartAt",
                table: "Ventanas",
                columns: new[] { "IngenioId", "StartAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Camiones");

            migrationBuilder.DropTable(
                name: "Fincas");

            migrationBuilder.DropTable(
                name: "Ingenios");

            migrationBuilder.DropTable(
                name: "TransportistaCamiones");

            migrationBuilder.DropTable(
                name: "Transportistas");

            migrationBuilder.DropTable(
                name: "Turnos");

            migrationBuilder.DropTable(
                name: "Ventanas");
        }
    }
}
