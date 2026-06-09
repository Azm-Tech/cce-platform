using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CCE.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Spring09DenormalizedCounters : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "share_count",
                table: "posts",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "view_count",
                table: "posts",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "follower_count",
                table: "communities",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "post_count",
                table: "communities",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "follower_count",
                table: "AspNetUsers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "following_count",
                table: "AspNetUsers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            // Backfill denormalized counters from existing relational rows.
            migrationBuilder.Sql(@"
                UPDATE AspNetUsers SET
                    follower_count = (SELECT COUNT(*) FROM user_follows WHERE followed_id = AspNetUsers.id),
                    following_count = (SELECT COUNT(*) FROM user_follows WHERE follower_id = AspNetUsers.id)
                WHERE id IN (SELECT id FROM AspNetUsers);
            ");

            migrationBuilder.Sql(@"
                UPDATE communities SET
                    follower_count = (SELECT COUNT(*) FROM community_follows WHERE community_id = communities.id),
                    post_count = (SELECT COUNT(*) FROM posts WHERE community_id = communities.id AND status = 1)
                WHERE id IN (SELECT id FROM communities);
            ");

            migrationBuilder.Sql(@"
                UPDATE posts SET
                    view_count = 0,
                    share_count = 0
                WHERE id IN (SELECT id FROM posts);
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "share_count",
                table: "posts");

            migrationBuilder.DropColumn(
                name: "view_count",
                table: "posts");

            migrationBuilder.DropColumn(
                name: "follower_count",
                table: "communities");

            migrationBuilder.DropColumn(
                name: "post_count",
                table: "communities");

            migrationBuilder.DropColumn(
                name: "follower_count",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "following_count",
                table: "AspNetUsers");
        }
    }
}
