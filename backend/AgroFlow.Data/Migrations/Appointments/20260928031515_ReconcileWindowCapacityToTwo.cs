using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgroFlow.Data.Migrations.Appointments
{
    /// <inheritdoc />
    public partial class ReconcileWindowCapacityToTwo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // openspec/specs/turnos-solicitud-y-asignacion/spec.md fija cupo
            // de dos camiones por ventana; el seed anterior sembró 1. Subir
            // Capacity nunca viola CK_Ventanas_Occupied_Range (Occupied no se
            // toca), así que es seguro reconciliar bases ya sembradas.
            migrationBuilder.Sql("UPDATE Ventanas SET Capacity = 2 WHERE Capacity = 1;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No reversible de forma segura: revertir a 1 podría dejar
            // Occupied > Capacity si ya se usó el cupo nuevo.
        }
    }
}
