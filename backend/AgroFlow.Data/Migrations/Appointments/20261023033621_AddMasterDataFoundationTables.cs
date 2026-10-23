using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgroFlow.Data.Migrations.Appointments
{
    /// <inheritdoc />
    public partial class AddMasterDataFoundationTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Transportistas_IngenioId_NormalizedWhatsApp",
                table: "Transportistas");

            migrationBuilder.CreateTable(
                name: "AuditEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    IngenioId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ActorIdentity = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    OccurredAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Action = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    EntityType = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    EntityId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Result = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    CorrelationId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Reason = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    DetailsReference = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IdempotencyRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Scope = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    ActorIdentity = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Operation = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Key = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    InputHash = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    ResultCode = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    ResultReference = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IdempotencyRecords", x => x.Id);
                });

            // DNI y WhatsApp pasan a ser únicos en todo el sistema. Si una base
            // existente ya los repite entre ingenios, la migración aborta con
            // el nombre del check como mensaje y no borra ni modifica datos:
            // los duplicados se resuelven a mano antes de volver a migrar.
            migrationBuilder.Sql(
                "CREATE TEMP TABLE \"__AgroFlowIdentifierPrecheck\" (" +
                "\"DuplicatedTransportistaIdentifiers\" INTEGER NOT NULL " +
                "CONSTRAINT \"resolve_duplicated_transportista_dni_or_whatsapp_before_migrating\" " +
                "CHECK (\"DuplicatedTransportistaIdentifiers\" = 0));");
            migrationBuilder.Sql(
                "INSERT INTO \"__AgroFlowIdentifierPrecheck\" SELECT " +
                "(SELECT COUNT(*) FROM (SELECT 1 FROM \"Transportistas\" GROUP BY \"NormalizedWhatsApp\" HAVING COUNT(*) > 1)) + " +
                "(SELECT COUNT(*) FROM (SELECT 1 FROM \"Transportistas\" GROUP BY \"NormalizedDni\" HAVING COUNT(*) > 1));");
            migrationBuilder.Sql("DROP TABLE \"__AgroFlowIdentifierPrecheck\";");

            migrationBuilder.CreateIndex(
                name: "IX_Transportistas_IngenioId",
                table: "Transportistas",
                column: "IngenioId");

            migrationBuilder.CreateIndex(
                name: "IX_Transportistas_NormalizedDni",
                table: "Transportistas",
                column: "NormalizedDni",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Transportistas_NormalizedWhatsApp",
                table: "Transportistas",
                column: "NormalizedWhatsApp",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AuditEvents_IngenioId_OccurredAtUtc",
                table: "AuditEvents",
                columns: new[] { "IngenioId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_IdempotencyRecords_Scope_ActorIdentity_Operation_Key",
                table: "IdempotencyRecords",
                columns: new[] { "Scope", "ActorIdentity", "Operation", "Key" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditEvents");

            migrationBuilder.DropTable(
                name: "IdempotencyRecords");

            migrationBuilder.DropIndex(
                name: "IX_Transportistas_IngenioId",
                table: "Transportistas");

            migrationBuilder.DropIndex(
                name: "IX_Transportistas_NormalizedDni",
                table: "Transportistas");

            migrationBuilder.DropIndex(
                name: "IX_Transportistas_NormalizedWhatsApp",
                table: "Transportistas");

            migrationBuilder.CreateIndex(
                name: "IX_Transportistas_IngenioId_NormalizedWhatsApp",
                table: "Transportistas",
                columns: new[] { "IngenioId", "NormalizedWhatsApp" },
                unique: true);
        }
    }
}
