using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CCE.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveSlugAddTagsToInteractiveMap : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_interactive_map_slug",
                table: "interactive_maps");

            migrationBuilder.DropColumn(
                name: "slug",
                table: "interactive_maps");

            migrationBuilder.CreateTable(
                name: "interactive_map_tag",
                columns: table => new
                {
                    interactive_map_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    tags_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_interactive_map_tag", x => new { x.interactive_map_id, x.tags_id });
                    table.ForeignKey(
                        name: "fk_interactive_map_tag_interactive_maps_interactive_map_id",
                        column: x => x.interactive_map_id,
                        principalTable: "interactive_maps",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_interactive_map_tag_tags_tags_id",
                        column: x => x.tags_id,
                        principalTable: "tags",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_interactive_map_tag_tags_id",
                table: "interactive_map_tag",
                column: "tags_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "interactive_map_tag");

            migrationBuilder.AddColumn<string>(
                name: "slug",
                table: "interactive_maps",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "ux_interactive_map_slug",
                table: "interactive_maps",
                column: "slug",
                unique: true);
        }
    }
}
