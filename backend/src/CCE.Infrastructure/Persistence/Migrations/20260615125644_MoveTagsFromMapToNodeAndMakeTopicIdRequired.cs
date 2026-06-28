using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CCE.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MoveTagsFromMapToNodeAndMakeTopicIdRequired : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "interactive_map_tag");

            migrationBuilder.DropColumn(
                name: "topic_slug",
                table: "interactive_map_nodes");

            migrationBuilder.AlterColumn<Guid>(
                name: "topic_id",
                table: "interactive_map_nodes",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "interactive_map_node_tag",
                columns: table => new
                {
                    interactive_map_node_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    tags_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_interactive_map_node_tag", x => new { x.interactive_map_node_id, x.tags_id });
                    table.ForeignKey(
                        name: "fk_interactive_map_node_tag_interactive_map_nodes_interactive_map_node_id",
                        column: x => x.interactive_map_node_id,
                        principalTable: "interactive_map_nodes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_interactive_map_node_tag_tags_tags_id",
                        column: x => x.tags_id,
                        principalTable: "tags",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_interactive_map_node_tag_tags_id",
                table: "interactive_map_node_tag",
                column: "tags_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "interactive_map_node_tag");

            migrationBuilder.AlterColumn<Guid>(
                name: "topic_id",
                table: "interactive_map_nodes",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddColumn<string>(
                name: "topic_slug",
                table: "interactive_map_nodes",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

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
    }
}
