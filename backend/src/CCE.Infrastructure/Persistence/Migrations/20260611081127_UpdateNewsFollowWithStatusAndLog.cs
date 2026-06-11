using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CCE.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UpdateNewsFollowWithStatusAndLog : Migration
    {
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(
                name: "created_by_id",
                table: "news_follows",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "last_modified_by_id",
                table: "news_follows",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "last_modified_on",
                table: "news_follows",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "status",
                table: "news_follows",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "unfollowed_on",
                table: "news_follows",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "news_notification_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    news_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    sent_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    status = table.Column<int>(type: "int", nullable: false),
                    content = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    created_on = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    created_by_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    last_modified_on = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    last_modified_by_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_news_notification_logs", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_news_notification_log_news_status",
                table: "news_notification_logs",
                columns: new[] { "news_id", "status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "news_notification_logs");

            migrationBuilder.DropColumn(
                name: "created_by_id",
                table: "news_follows");

            migrationBuilder.DropColumn(
                name: "last_modified_by_id",
                table: "news_follows");

            migrationBuilder.DropColumn(
                name: "last_modified_on",
                table: "news_follows");

            migrationBuilder.DropColumn(
                name: "status",
                table: "news_follows");

            migrationBuilder.DropColumn(
                name: "unfollowed_on",
                table: "news_follows");
        }
    }
}
