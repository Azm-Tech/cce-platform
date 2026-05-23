using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CCE.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationGateway : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_notification_template_code",
                table: "notification_templates");

            migrationBuilder.CreateTable(
                name: "notification_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    recipient_user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    template_code = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    template_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    channel = table.Column<int>(type: "int", nullable: false),
                    status = table.Column<int>(type: "int", nullable: false),
                    provider_message_id = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    error = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    attempt_count = table.Column<int>(type: "int", nullable: false),
                    created_on = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    sent_on = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    failed_on = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    correlation_id = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    payload_json = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_notification_logs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "user_notification_settings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    channel = table.Column<int>(type: "int", nullable: false),
                    event_code = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    is_enabled = table.Column<bool>(type: "bit", nullable: false),
                    updated_on = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_notification_settings", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ux_notification_template_code_channel",
                table: "notification_templates",
                columns: new[] { "code", "channel" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_notification_log_correlation_id",
                table: "notification_logs",
                column: "correlation_id");

            migrationBuilder.CreateIndex(
                name: "ix_notification_log_recipient_status_created",
                table: "notification_logs",
                columns: new[] { "recipient_user_id", "status", "created_on" });

            migrationBuilder.CreateIndex(
                name: "ix_notification_log_template_channel",
                table: "notification_logs",
                columns: new[] { "template_code", "channel" });

            migrationBuilder.CreateIndex(
                name: "ux_user_notification_settings_user_channel_event",
                table: "user_notification_settings",
                columns: new[] { "user_id", "channel", "event_code" },
                unique: true,
                filter: "[event_code] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "notification_logs");

            migrationBuilder.DropTable(
                name: "user_notification_settings");

            migrationBuilder.DropIndex(
                name: "ux_notification_template_code_channel",
                table: "notification_templates");

            migrationBuilder.CreateIndex(
                name: "ux_notification_template_code",
                table: "notification_templates",
                column: "code",
                unique: true);
        }
    }
}
