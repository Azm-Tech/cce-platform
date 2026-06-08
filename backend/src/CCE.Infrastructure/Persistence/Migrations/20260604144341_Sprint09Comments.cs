using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CCE.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Sprint09Comments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "child_count",
                table: "post_replies",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "depth",
                table: "post_replies",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "thread_path",
                table: "post_replies",
                type: "nvarchar(900)",
                maxLength: 900,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "mentions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    source_type = table.Column<int>(type: "int", nullable: false),
                    source_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    mentioned_user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    mentioned_by_user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    created_on = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_mentions", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_post_reply_thread_path",
                table: "post_replies",
                column: "thread_path");

            migrationBuilder.CreateIndex(
                name: "ix_mention_user_created",
                table: "mentions",
                columns: new[] { "mentioned_user_id", "created_on" });

            migrationBuilder.CreateIndex(
                name: "ux_mention_source_user",
                table: "mentions",
                columns: new[] { "source_type", "source_id", "mentioned_user_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "mentions");

            migrationBuilder.DropIndex(
                name: "ix_post_reply_thread_path",
                table: "post_replies");

            migrationBuilder.DropColumn(
                name: "child_count",
                table: "post_replies");

            migrationBuilder.DropColumn(
                name: "depth",
                table: "post_replies");

            migrationBuilder.DropColumn(
                name: "thread_path",
                table: "post_replies");
        }
    }
}
