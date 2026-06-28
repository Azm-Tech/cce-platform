using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CCE.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceNewsSlugsWithTags : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_news_slug_active",
                table: "news");

            migrationBuilder.DropColumn(
                name: "slug",
                table: "news");

            migrationBuilder.CreateTable(
                name: "tags",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    name_ar = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    name_en = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    color = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tags", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "event_tag",
                columns: table => new
                {
                    event_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    tags_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_event_tag", x => new { x.event_id, x.tags_id });
                    table.ForeignKey(
                        name: "fk_event_tag_events_event_id",
                        column: x => x.event_id,
                        principalTable: "events",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_event_tag_tags_tags_id",
                        column: x => x.tags_id,
                        principalTable: "tags",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "news_tag",
                columns: table => new
                {
                    news_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    tags_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_news_tag", x => new { x.news_id, x.tags_id });
                    table.ForeignKey(
                        name: "fk_news_tag_news_news_id",
                        column: x => x.news_id,
                        principalTable: "news",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_news_tag_tags_tags_id",
                        column: x => x.tags_id,
                        principalTable: "tags",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_event_tag_tags_id",
                table: "event_tag",
                column: "tags_id");

            migrationBuilder.CreateIndex(
                name: "ix_news_tag_tags_id",
                table: "news_tag",
                column: "tags_id");

            migrationBuilder.CreateIndex(
                name: "ux_tag_name_en",
                table: "tags",
                column: "name_en",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "event_tag");

            migrationBuilder.DropTable(
                name: "news_tag");

            migrationBuilder.DropTable(
                name: "tags");

            migrationBuilder.AddColumn<string>(
                name: "slug",
                table: "news",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "ux_news_slug_active",
                table: "news",
                column: "slug",
                unique: true,
                filter: "[is_deleted] = 0");
        }
    }
}
