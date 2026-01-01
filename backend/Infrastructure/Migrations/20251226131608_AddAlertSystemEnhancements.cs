using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAlertSystemEnhancements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AlertRules_MonitoredServerId",
                table: "AlertRules");

            migrationBuilder.AlterColumn<int>(
                name: "MonitoredServerId",
                table: "AlertRules",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CooldownMinutes",
                table: "AlertRules",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "TargetId",
                table: "AlertRules",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TargetType",
                table: "AlertRules",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "NotificationSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TelegramBotToken = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsTelegramEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TelegramChatIds",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ChatId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Label = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    NotificationSettingsId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TelegramChatIds", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TelegramChatIds_NotificationSettings_NotificationSettingsId",
                        column: x => x.NotificationSettingsId,
                        principalTable: "NotificationSettings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AlertRules_MonitoredServerId_IsActive",
                table: "AlertRules",
                columns: new[] { "MonitoredServerId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_TelegramChatIds_ChatId",
                table: "TelegramChatIds",
                column: "ChatId");

            migrationBuilder.CreateIndex(
                name: "IX_TelegramChatIds_NotificationSettingsId",
                table: "TelegramChatIds",
                column: "NotificationSettingsId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TelegramChatIds");

            migrationBuilder.DropTable(
                name: "NotificationSettings");

            migrationBuilder.DropIndex(
                name: "IX_AlertRules_MonitoredServerId_IsActive",
                table: "AlertRules");

            migrationBuilder.DropColumn(
                name: "CooldownMinutes",
                table: "AlertRules");

            migrationBuilder.DropColumn(
                name: "TargetId",
                table: "AlertRules");

            migrationBuilder.DropColumn(
                name: "TargetType",
                table: "AlertRules");

            migrationBuilder.AlterColumn<int>(
                name: "MonitoredServerId",
                table: "AlertRules",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.CreateIndex(
                name: "IX_AlertRules_MonitoredServerId",
                table: "AlertRules",
                column: "MonitoredServerId");
        }
    }
}
