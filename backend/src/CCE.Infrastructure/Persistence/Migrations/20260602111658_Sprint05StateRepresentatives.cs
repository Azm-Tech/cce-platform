using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CCE.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Sprint05StateRepresentatives : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "country_resource_requests");

            migrationBuilder.AddColumn<decimal>(
                name: "area_sq_km",
                table: "country_profiles",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "gdp_per_capita",
                table: "country_profiles",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "nationally_determined_contribution_asset_id",
                table: "country_profiles",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "population",
                table: "country_profiles",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "country_content_requests",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    country_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    requested_by_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    kind = table.Column<int>(type: "int", nullable: false),
                    status = table.Column<int>(type: "int", nullable: false),
                    proposed_title_ar = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    proposed_title_en = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    proposed_description_ar = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    proposed_description_en = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    proposed_resource_type = table.Column<int>(type: "int", nullable: true),
                    proposed_asset_file_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    proposed_topic_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    proposed_starts_on = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    proposed_ends_on = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    proposed_location_ar = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    proposed_location_en = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    proposed_online_meeting_url = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    submitted_on = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    admin_notes_ar = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    admin_notes_en = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    processed_by_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    processed_on = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
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
                    table.PrimaryKey("pk_country_content_requests", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_country_content_request_country_status_kind",
                table: "country_content_requests",
                columns: new[] { "country_id", "status", "kind" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "country_content_requests");

            migrationBuilder.DropColumn(
                name: "area_sq_km",
                table: "country_profiles");

            migrationBuilder.DropColumn(
                name: "gdp_per_capita",
                table: "country_profiles");

            migrationBuilder.DropColumn(
                name: "nationally_determined_contribution_asset_id",
                table: "country_profiles");

            migrationBuilder.DropColumn(
                name: "population",
                table: "country_profiles");

            migrationBuilder.CreateTable(
                name: "country_resource_requests",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    admin_notes_ar = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    admin_notes_en = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    country_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    created_by_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    created_on = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    deleted_by_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    deleted_on = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false),
                    last_modified_by_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    last_modified_on = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    processed_by_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    processed_on = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    proposed_asset_file_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    proposed_description_ar = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    proposed_description_en = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    proposed_resource_type = table.Column<int>(type: "int", nullable: false),
                    proposed_title_ar = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    proposed_title_en = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    requested_by_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    status = table.Column<int>(type: "int", nullable: false),
                    submitted_on = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_country_resource_requests", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_country_request_country_status",
                table: "country_resource_requests",
                columns: new[] { "country_id", "status" });
        }
    }
}
