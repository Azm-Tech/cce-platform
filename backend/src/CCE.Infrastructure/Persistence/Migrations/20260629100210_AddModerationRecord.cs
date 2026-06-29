using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CCE.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddModerationRecord : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "moderation_status",
                table: "posts",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "moderation_status",
                table: "post_replies",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "moderation_record",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    content_type = table.Column<int>(type: "int", nullable: false),
                    content_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    status = table.Column<int>(type: "int", nullable: false),
                    phase = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    provider = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    score = table.Column<float>(type: "real", nullable: true),
                    category = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    reason = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    reviewed_by_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    created_on = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_moderation_record", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_moderation_record_content",
                table: "moderation_record",
                columns: new[] { "content_type", "content_id" });

            migrationBuilder.CreateIndex(
                name: "ix_moderation_record_status",
                table: "moderation_record",
                column: "status");

            // Backfill: existing published posts and non-deleted replies are pre-approved
            // so they do not flood the moderation consumer queue on first deploy.
            migrationBuilder.Sql("UPDATE posts SET moderation_status = 1 WHERE status = 1");
            migrationBuilder.Sql("UPDATE post_replies SET moderation_status = 1 WHERE is_deleted = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "moderation_record");

            migrationBuilder.DropColumn(
                name: "moderation_status",
                table: "posts");

            migrationBuilder.DropColumn(
                name: "moderation_status",
                table: "post_replies");
        }
    }
}
