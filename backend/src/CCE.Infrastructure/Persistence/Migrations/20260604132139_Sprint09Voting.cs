using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CCE.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Sprint09Voting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "post_ratings");

            migrationBuilder.DropIndex(
                name: "ix_post_reply_post_id",
                table: "post_replies");

            migrationBuilder.AddColumn<int>(
                name: "downvote_count",
                table: "posts",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<double>(
                name: "score",
                table: "posts",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<int>(
                name: "upvote_count",
                table: "posts",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "downvote_count",
                table: "post_replies",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<double>(
                name: "score",
                table: "post_replies",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<int>(
                name: "upvote_count",
                table: "post_replies",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "post_votes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    post_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    value = table.Column<int>(type: "int", nullable: false),
                    voted_on = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_post_votes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "reply_votes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    reply_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    value = table.Column<int>(type: "int", nullable: false),
                    voted_on = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_reply_votes", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_post_score",
                table: "posts",
                column: "score",
                descending: new[] { true });

            migrationBuilder.CreateIndex(
                name: "ix_post_reply_post_score",
                table: "post_replies",
                columns: new[] { "post_id", "score" });

            migrationBuilder.CreateIndex(
                name: "ux_post_vote_post_user",
                table: "post_votes",
                columns: new[] { "post_id", "user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_reply_vote_reply_user",
                table: "reply_votes",
                columns: new[] { "reply_id", "user_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "post_votes");

            migrationBuilder.DropTable(
                name: "reply_votes");

            migrationBuilder.DropIndex(
                name: "ix_post_score",
                table: "posts");

            migrationBuilder.DropIndex(
                name: "ix_post_reply_post_score",
                table: "post_replies");

            migrationBuilder.DropColumn(
                name: "downvote_count",
                table: "posts");

            migrationBuilder.DropColumn(
                name: "score",
                table: "posts");

            migrationBuilder.DropColumn(
                name: "upvote_count",
                table: "posts");

            migrationBuilder.DropColumn(
                name: "downvote_count",
                table: "post_replies");

            migrationBuilder.DropColumn(
                name: "score",
                table: "post_replies");

            migrationBuilder.DropColumn(
                name: "upvote_count",
                table: "post_replies");

            migrationBuilder.CreateTable(
                name: "post_ratings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    post_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    rated_on = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    stars = table.Column<int>(type: "int", nullable: false),
                    user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_post_ratings", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_post_reply_post_id",
                table: "post_replies",
                column: "post_id");

            migrationBuilder.CreateIndex(
                name: "ux_post_rating_post_user",
                table: "post_ratings",
                columns: new[] { "post_id", "user_id" },
                unique: true);
        }
    }
}
