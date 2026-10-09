using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgroFlow.Data.Migrations.Appointments
{
    /// <inheritdoc />
    public partial class AddActiveAssociationUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_TransportistaCamiones_CamionId",
                table: "TransportistaCamiones",
                column: "CamionId",
                unique: true,
                filter: "IsActive = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TransportistaCamiones_CamionId",
                table: "TransportistaCamiones");
        }
    }
}
