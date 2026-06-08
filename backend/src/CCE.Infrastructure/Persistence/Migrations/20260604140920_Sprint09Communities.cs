using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CCE.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Sprint09Communities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "community_id",
                table: "posts",
                type: "uniqueidentifier",
                nullable: false,
                // Backfill pre-existing posts into the seeded "General" community (CommunitySeedIds.GeneralCommunityId).
                defaultValue: new Guid("c0ffee00-0000-0000-0000-000000000001"));

            migrationBuilder.CreateTable(
                name: "communities",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    name_ar = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    name_en = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    description_ar = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    description_en = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    slug = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    visibility = table.Column<int>(type: "int", nullable: false),
                    presentation_json = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    member_count = table.Column<int>(type: "int", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                    created_on = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    created_by_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    last_modified_on = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    last_modified_by_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false),
                    deleted_on = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    deleted_by_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_communities", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "community_follows",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    community_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    followed_on = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_community_follows", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "community_join_requests",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    community_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    status = table.Column<int>(type: "int", nullable: false),
                    requested_on = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    decided_by_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    decided_on = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_community_join_requests", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "community_memberships",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    community_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    role = table.Column<int>(type: "int", nullable: false),
                    joined_on = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_community_memberships", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_post_community_score",
                table: "posts",
                columns: new[] { "community_id", "score" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ux_community_slug_active",
                table: "communities",
                column: "slug",
                unique: true,
                filter: "[is_deleted] = 0");

            migrationBuilder.CreateIndex(
                name: "ux_community_follow_community_user",
                table: "community_follows",
                columns: new[] { "community_id", "user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_community_join_request_community_status",
                table: "community_join_requests",
                columns: new[] { "community_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ux_community_join_request_pending",
                table: "community_join_requests",
                columns: new[] { "community_id", "user_id" },
                unique: true,
                filter: "[status] = 0");

            migrationBuilder.CreateIndex(
                name: "ux_community_membership_community_user",
                table: "community_memberships",
                columns: new[] { "community_id", "user_id" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_posts_communities_community_id",
                table: "posts",
                column: "community_id",
                principalTable: "communities",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_posts_communities_community_id",
                table: "posts");

            migrationBuilder.DropTable(
                name: "communities");

            migrationBuilder.DropTable(
                name: "community_follows");

            migrationBuilder.DropTable(
                name: "community_join_requests");

            migrationBuilder.DropTable(
                name: "community_memberships");

            migrationBuilder.DropIndex(
                name: "ix_post_community_score",
                table: "posts");

            migrationBuilder.DropColumn(
                name: "community_id",
                table: "posts");
        }
    }
}
