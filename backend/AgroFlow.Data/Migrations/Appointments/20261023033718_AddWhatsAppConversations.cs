using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgroFlow.Data.Migrations.Appointments
{
    /// <inheritdoc />
    public partial class AddWhatsAppConversations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ChannelInstances",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    IngenioId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Provider = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    InstanceName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChannelInstances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChannelInstances_Ingenios_IngenioId",
                        column: x => x.IngenioId,
                        principalTable: "Ingenios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Conversations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    IngenioId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ChannelInstanceId = table.Column<Guid>(type: "TEXT", nullable: false),
                    RemoteAddress = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    AddressKind = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    NormalizedPhone = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    TransportistaId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastMessageAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Conversations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Conversations_ChannelInstances_ChannelInstanceId",
                        column: x => x.ChannelInstanceId,
                        principalTable: "ChannelInstances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Conversations_Ingenios_IngenioId",
                        column: x => x.IngenioId,
                        principalTable: "Ingenios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Conversations_Transportistas_TransportistaId",
                        column: x => x.TransportistaId,
                        principalTable: "Transportistas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ConversationMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    IngenioId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ConversationId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Direction = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    ProviderMessageId = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    ContentType = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Text = table.Column<string>(type: "TEXT", maxLength: 4096, nullable: false),
                    ProviderTimestampUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    PersistedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    InReplyToMessageId = table.Column<Guid>(type: "TEXT", nullable: true),
                    SendStatus = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    SendStatusUpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConversationMessages", x => x.Id);
                    table.CheckConstraint("CK_ConversationMessages_Direction_Shape", "(Direction = 'Inbound' AND ProviderMessageId IS NOT NULL AND SendStatus IS NULL AND InReplyToMessageId IS NULL) OR (Direction = 'Outbound' AND SendStatus IS NOT NULL AND InReplyToMessageId IS NOT NULL)");
                    table.CheckConstraint("CK_ConversationMessages_Text_Length", "length(Text) BETWEEN 1 AND 4096");
                    table.ForeignKey(
                        name: "FK_ConversationMessages_ConversationMessages_InReplyToMessageId",
                        column: x => x.InReplyToMessageId,
                        principalTable: "ConversationMessages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ConversationMessages_Conversations_ConversationId",
                        column: x => x.ConversationId,
                        principalTable: "Conversations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MessageProcessings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    IngenioId = table.Column<Guid>(type: "TEXT", nullable: false),
                    InboundMessageId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    ClaimTokenHash = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    ClaimedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ClaimExpiresAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LastErrorCode = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MessageProcessings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MessageProcessings_ConversationMessages_InboundMessageId",
                        column: x => x.InboundMessageId,
                        principalTable: "ConversationMessages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChannelInstances_IngenioId",
                table: "ChannelInstances",
                column: "IngenioId");

            migrationBuilder.CreateIndex(
                name: "IX_ChannelInstances_Provider_InstanceName",
                table: "ChannelInstances",
                columns: new[] { "Provider", "InstanceName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ConversationMessages_ConversationId_Direction_ProviderMessageId",
                table: "ConversationMessages",
                columns: new[] { "ConversationId", "Direction", "ProviderMessageId" },
                unique: true,
                filter: "ProviderMessageId IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ConversationMessages_ConversationId_PersistedAtUtc",
                table: "ConversationMessages",
                columns: new[] { "ConversationId", "PersistedAtUtc" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ConversationMessages_InReplyToMessageId",
                table: "ConversationMessages",
                column: "InReplyToMessageId",
                unique: true,
                filter: "InReplyToMessageId IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Conversations_ChannelInstanceId_RemoteAddress",
                table: "Conversations",
                columns: new[] { "ChannelInstanceId", "RemoteAddress" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Conversations_IngenioId_LastMessageAtUtc",
                table: "Conversations",
                columns: new[] { "IngenioId", "LastMessageAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Conversations_TransportistaId",
                table: "Conversations",
                column: "TransportistaId");

            migrationBuilder.CreateIndex(
                name: "IX_MessageProcessings_InboundMessageId",
                table: "MessageProcessings",
                column: "InboundMessageId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MessageProcessings_IngenioId_Status",
                table: "MessageProcessings",
                columns: new[] { "IngenioId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MessageProcessings");

            migrationBuilder.DropTable(
                name: "ConversationMessages");

            migrationBuilder.DropTable(
                name: "Conversations");

            migrationBuilder.DropTable(
                name: "ChannelInstances");
        }
    }
}
