using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CCE.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddModerationQueueIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_moderation_record_content",
                table: "moderation_record");

            migrationBuilder.CreateIndex(
                name: "ix_post_moderation_status",
                table: "posts",
                column: "moderation_status");

            migrationBuilder.CreateIndex(
                name: "ix_post_reply_moderation_status",
                table: "post_replies",
                column: "moderation_status");

            migrationBuilder.CreateIndex(
                name: "ix_moderation_record_content_created",
                table: "moderation_record",
                columns: new[] { "content_type", "content_id", "created_on" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_post_moderation_status",
                table: "posts");

            migrationBuilder.DropIndex(
                name: "ix_post_reply_moderation_status",
                table: "post_replies");

            migrationBuilder.DropIndex(
                name: "ix_moderation_record_content_created",
                table: "moderation_record");

            migrationBuilder.CreateIndex(
                name: "ix_moderation_record_content",
                table: "moderation_record",
                columns: new[] { "content_type", "content_id" });
        }
    }
}
