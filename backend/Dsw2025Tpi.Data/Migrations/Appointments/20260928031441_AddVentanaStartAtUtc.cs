using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dsw2025Tpi.Data.Migrations.Appointments
{
    /// <inheritdoc />
    public partial class AddVentanaStartAtUtc : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "StartAtUtc",
                table: "Ventanas",
                type: "TEXT",
                nullable: true);

            // Reconcilia filas sembradas antes de esta columna. datetime()
            // interpreta el desplazamiento explícito ya presente en StartAt
            // (texto ISO 8601, ej. "2026-09-28 08:00:00-03:00") y normaliza a
            // UTC; no depende de una constante de offset propia.
            migrationBuilder.Sql("UPDATE Ventanas SET StartAtUtc = datetime(StartAt) WHERE StartAtUtc IS NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StartAtUtc",
                table: "Ventanas");
        }
    }
}
