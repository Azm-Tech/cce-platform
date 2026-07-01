using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CCE.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveAndReaddNodeTitleDescription : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "description_ar",
                table: "interactive_map_nodes");

            migrationBuilder.DropColumn(
                name: "description_en",
                table: "interactive_map_nodes");

            migrationBuilder.DropColumn(
                name: "title_ar",
                table: "interactive_map_nodes");

            migrationBuilder.DropColumn(
                name: "title_en",
                table: "interactive_map_nodes");

            migrationBuilder.AddColumn<string>(
                name: "title_ar",
                table: "interactive_map_nodes",
                type: "nvarchar(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "title_en",
                table: "interactive_map_nodes",
                type: "nvarchar(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "description_ar",
                table: "interactive_map_nodes",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "description_en",
                table: "interactive_map_nodes",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "description_ar",
                table: "interactive_map_nodes");

            migrationBuilder.DropColumn(
                name: "description_en",
                table: "interactive_map_nodes");

            migrationBuilder.DropColumn(
                name: "title_ar",
                table: "interactive_map_nodes");

            migrationBuilder.DropColumn(
                name: "title_en",
                table: "interactive_map_nodes");

            migrationBuilder.AddColumn<string>(
                name: "title_ar",
                table: "interactive_map_nodes",
                type: "nvarchar(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "title_en",
                table: "interactive_map_nodes",
                type: "nvarchar(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "description_ar",
                table: "interactive_map_nodes",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "description_en",
                table: "interactive_map_nodes",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
