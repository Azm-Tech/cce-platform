using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CCE.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProposedCategoryIdToContentRequest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "interests",
                table: "AspNetUsers");

            migrationBuilder.AddColumn<Guid>(
                name: "proposed_category_id",
                table: "country_content_requests",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "interest_topics",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    name_ar = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    name_en = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    category = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_interest_topics", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "user_interest_topics",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    interest_topic_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_interest_topics", x => new { x.user_id, x.interest_topic_id });
                    table.ForeignKey(
                        name: "fk_user_interest_topics_interest_topics_interest_topic_id",
                        column: x => x.interest_topic_id,
                        principalTable: "interest_topics",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_user_interest_topics_users_user_id",
                        column: x => x.user_id,
                        principalTable: "AspNetUsers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_user_interest_topics_interest_topic_id",
                table: "user_interest_topics",
                column: "interest_topic_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "user_interest_topics");

            migrationBuilder.DropTable(
                name: "interest_topics");

            migrationBuilder.DropColumn(
                name: "proposed_category_id",
                table: "country_content_requests");

            migrationBuilder.AddColumn<string>(
                name: "interests",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
