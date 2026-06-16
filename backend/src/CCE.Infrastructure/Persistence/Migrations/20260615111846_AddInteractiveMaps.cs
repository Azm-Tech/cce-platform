using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CCE.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddInteractiveMaps : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "interactive_map_nodes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    interactive_map_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    name_ar = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    name_en = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    icon_key = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    category = table.Column<int>(type: "int", nullable: true),
                    category_name_ar = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    category_name_en = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    level = table.Column<int>(type: "int", nullable: false),
                    parent_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    topic_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    topic_slug = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    is_active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_interactive_map_nodes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "interactive_maps",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    name_ar = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    name_en = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    description_ar = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    description_en = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    slug = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_interactive_maps", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_interactive_map_node_map_id",
                table: "interactive_map_nodes",
                column: "interactive_map_id");

            migrationBuilder.CreateIndex(
                name: "ix_interactive_map_node_parent_id",
                table: "interactive_map_nodes",
                column: "parent_id");

            migrationBuilder.CreateIndex(
                name: "ix_interactive_map_node_topic_id",
                table: "interactive_map_nodes",
                column: "topic_id");

            migrationBuilder.CreateIndex(
                name: "ux_interactive_map_slug",
                table: "interactive_maps",
                column: "slug",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "interactive_map_nodes");

            migrationBuilder.DropTable(
                name: "interactive_maps");
        }
    }
}
