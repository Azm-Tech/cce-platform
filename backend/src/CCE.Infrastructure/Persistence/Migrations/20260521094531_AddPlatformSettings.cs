using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CCE.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPlatformSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "about_settings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    description_ar = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    description_en = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    how_to_use_video_url = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
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
                    table.PrimaryKey("pk_about_settings", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "glossary_entries",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    about_settings_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    term_ar = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    term_en = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    definition_ar = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    definition_en = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    order_index = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("pk_glossary_entries", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "homepage_countries",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    homepage_settings_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    country_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    order_index = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_homepage_countries", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "homepage_settings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    video_url = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    objective_ar = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    objective_en = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    cce_concepts_ar = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    cce_concepts_en = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
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
                    table.PrimaryKey("pk_homepage_settings", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "knowledge_partners",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    about_settings_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    name_ar = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    name_en = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    logo_url = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    website_url = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    description_ar = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    description_en = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    order_index = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("pk_knowledge_partners", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "policies_settings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
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
                    table.PrimaryKey("pk_policies_settings", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "policy_sections",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    policies_settings_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    type = table.Column<int>(type: "int", nullable: false),
                    title_ar = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    title_en = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    content_ar = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    content_en = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    order_index = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("pk_policy_sections", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_homepage_country_settings_country",
                table: "homepage_countries",
                columns: new[] { "homepage_settings_id", "country_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "about_settings");

            migrationBuilder.DropTable(
                name: "glossary_entries");

            migrationBuilder.DropTable(
                name: "homepage_countries");

            migrationBuilder.DropTable(
                name: "homepage_settings");

            migrationBuilder.DropTable(
                name: "knowledge_partners");

            migrationBuilder.DropTable(
                name: "policies_settings");

            migrationBuilder.DropTable(
                name: "policy_sections");
        }
    }
}
