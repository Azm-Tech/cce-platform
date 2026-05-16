using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CCE.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class StandardizeCountryProfileAudit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "last_updated_on",
                table: "country_profiles",
                newName: "created_on");

            migrationBuilder.RenameColumn(
                name: "last_updated_by_id",
                table: "country_profiles",
                newName: "created_by_id");

            migrationBuilder.AddColumn<Guid>(
                name: "created_by_id",
                table: "topics",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "created_on",
                table: "topics",
                type: "datetimeoffset",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<Guid>(
                name: "last_modified_by_id",
                table: "topics",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "last_modified_on",
                table: "topics",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "created_by_id",
                table: "state_representative_assignments",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "created_on",
                table: "state_representative_assignments",
                type: "datetimeoffset",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<Guid>(
                name: "last_modified_by_id",
                table: "state_representative_assignments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "last_modified_on",
                table: "state_representative_assignments",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "created_by_id",
                table: "resources",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "created_on",
                table: "resources",
                type: "datetimeoffset",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<Guid>(
                name: "last_modified_by_id",
                table: "resources",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "last_modified_on",
                table: "resources",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "created_by_id",
                table: "posts",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "last_modified_by_id",
                table: "posts",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "last_modified_on",
                table: "posts",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "created_by_id",
                table: "post_replies",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "last_modified_by_id",
                table: "post_replies",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "last_modified_on",
                table: "post_replies",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "created_by_id",
                table: "pages",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "created_on",
                table: "pages",
                type: "datetimeoffset",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<Guid>(
                name: "last_modified_by_id",
                table: "pages",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "last_modified_on",
                table: "pages",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "created_by_id",
                table: "news",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "created_on",
                table: "news",
                type: "datetimeoffset",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<Guid>(
                name: "last_modified_by_id",
                table: "news",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "last_modified_on",
                table: "news",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "created_by_id",
                table: "knowledge_maps",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "created_on",
                table: "knowledge_maps",
                type: "datetimeoffset",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<Guid>(
                name: "last_modified_by_id",
                table: "knowledge_maps",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "last_modified_on",
                table: "knowledge_maps",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "created_by_id",
                table: "homepage_sections",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "created_on",
                table: "homepage_sections",
                type: "datetimeoffset",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<Guid>(
                name: "last_modified_by_id",
                table: "homepage_sections",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "last_modified_on",
                table: "homepage_sections",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "created_by_id",
                table: "expert_registration_requests",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "created_on",
                table: "expert_registration_requests",
                type: "datetimeoffset",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<Guid>(
                name: "last_modified_by_id",
                table: "expert_registration_requests",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "last_modified_on",
                table: "expert_registration_requests",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "created_by_id",
                table: "expert_profiles",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "created_on",
                table: "expert_profiles",
                type: "datetimeoffset",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<Guid>(
                name: "last_modified_by_id",
                table: "expert_profiles",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "last_modified_on",
                table: "expert_profiles",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "created_by_id",
                table: "events",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "created_on",
                table: "events",
                type: "datetimeoffset",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<Guid>(
                name: "last_modified_by_id",
                table: "events",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "last_modified_on",
                table: "events",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "created_by_id",
                table: "country_resource_requests",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "created_on",
                table: "country_resource_requests",
                type: "datetimeoffset",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<Guid>(
                name: "last_modified_by_id",
                table: "country_resource_requests",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "last_modified_on",
                table: "country_resource_requests",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "last_modified_by_id",
                table: "country_profiles",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "last_modified_on",
                table: "country_profiles",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "created_by_id",
                table: "countries",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "created_on",
                table: "countries",
                type: "datetimeoffset",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<Guid>(
                name: "last_modified_by_id",
                table: "countries",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "last_modified_on",
                table: "countries",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "last_modified_on",
                table: "city_scenarios",
                type: "datetimeoffset",
                nullable: true,
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetimeoffset");

            migrationBuilder.AddColumn<Guid>(
                name: "created_by_id",
                table: "city_scenarios",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "last_modified_by_id",
                table: "city_scenarios",
                type: "uniqueidentifier",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "created_by_id",
                table: "topics");

            migrationBuilder.DropColumn(
                name: "created_on",
                table: "topics");

            migrationBuilder.DropColumn(
                name: "last_modified_by_id",
                table: "topics");

            migrationBuilder.DropColumn(
                name: "last_modified_on",
                table: "topics");

            migrationBuilder.DropColumn(
                name: "created_by_id",
                table: "state_representative_assignments");

            migrationBuilder.DropColumn(
                name: "created_on",
                table: "state_representative_assignments");

            migrationBuilder.DropColumn(
                name: "last_modified_by_id",
                table: "state_representative_assignments");

            migrationBuilder.DropColumn(
                name: "last_modified_on",
                table: "state_representative_assignments");

            migrationBuilder.DropColumn(
                name: "created_by_id",
                table: "resources");

            migrationBuilder.DropColumn(
                name: "created_on",
                table: "resources");

            migrationBuilder.DropColumn(
                name: "last_modified_by_id",
                table: "resources");

            migrationBuilder.DropColumn(
                name: "last_modified_on",
                table: "resources");

            migrationBuilder.DropColumn(
                name: "created_by_id",
                table: "posts");

            migrationBuilder.DropColumn(
                name: "last_modified_by_id",
                table: "posts");

            migrationBuilder.DropColumn(
                name: "last_modified_on",
                table: "posts");

            migrationBuilder.DropColumn(
                name: "created_by_id",
                table: "post_replies");

            migrationBuilder.DropColumn(
                name: "last_modified_by_id",
                table: "post_replies");

            migrationBuilder.DropColumn(
                name: "last_modified_on",
                table: "post_replies");

            migrationBuilder.DropColumn(
                name: "created_by_id",
                table: "pages");

            migrationBuilder.DropColumn(
                name: "created_on",
                table: "pages");

            migrationBuilder.DropColumn(
                name: "last_modified_by_id",
                table: "pages");

            migrationBuilder.DropColumn(
                name: "last_modified_on",
                table: "pages");

            migrationBuilder.DropColumn(
                name: "created_by_id",
                table: "news");

            migrationBuilder.DropColumn(
                name: "created_on",
                table: "news");

            migrationBuilder.DropColumn(
                name: "last_modified_by_id",
                table: "news");

            migrationBuilder.DropColumn(
                name: "last_modified_on",
                table: "news");

            migrationBuilder.DropColumn(
                name: "created_by_id",
                table: "knowledge_maps");

            migrationBuilder.DropColumn(
                name: "created_on",
                table: "knowledge_maps");

            migrationBuilder.DropColumn(
                name: "last_modified_by_id",
                table: "knowledge_maps");

            migrationBuilder.DropColumn(
                name: "last_modified_on",
                table: "knowledge_maps");

            migrationBuilder.DropColumn(
                name: "created_by_id",
                table: "homepage_sections");

            migrationBuilder.DropColumn(
                name: "created_on",
                table: "homepage_sections");

            migrationBuilder.DropColumn(
                name: "last_modified_by_id",
                table: "homepage_sections");

            migrationBuilder.DropColumn(
                name: "last_modified_on",
                table: "homepage_sections");

            migrationBuilder.DropColumn(
                name: "created_by_id",
                table: "expert_registration_requests");

            migrationBuilder.DropColumn(
                name: "created_on",
                table: "expert_registration_requests");

            migrationBuilder.DropColumn(
                name: "last_modified_by_id",
                table: "expert_registration_requests");

            migrationBuilder.DropColumn(
                name: "last_modified_on",
                table: "expert_registration_requests");

            migrationBuilder.DropColumn(
                name: "created_by_id",
                table: "expert_profiles");

            migrationBuilder.DropColumn(
                name: "created_on",
                table: "expert_profiles");

            migrationBuilder.DropColumn(
                name: "last_modified_by_id",
                table: "expert_profiles");

            migrationBuilder.DropColumn(
                name: "last_modified_on",
                table: "expert_profiles");

            migrationBuilder.DropColumn(
                name: "created_by_id",
                table: "events");

            migrationBuilder.DropColumn(
                name: "created_on",
                table: "events");

            migrationBuilder.DropColumn(
                name: "last_modified_by_id",
                table: "events");

            migrationBuilder.DropColumn(
                name: "last_modified_on",
                table: "events");

            migrationBuilder.DropColumn(
                name: "created_by_id",
                table: "country_resource_requests");

            migrationBuilder.DropColumn(
                name: "created_on",
                table: "country_resource_requests");

            migrationBuilder.DropColumn(
                name: "last_modified_by_id",
                table: "country_resource_requests");

            migrationBuilder.DropColumn(
                name: "last_modified_on",
                table: "country_resource_requests");

            migrationBuilder.DropColumn(
                name: "last_modified_by_id",
                table: "country_profiles");

            migrationBuilder.DropColumn(
                name: "last_modified_on",
                table: "country_profiles");

            migrationBuilder.DropColumn(
                name: "created_by_id",
                table: "countries");

            migrationBuilder.DropColumn(
                name: "created_on",
                table: "countries");

            migrationBuilder.DropColumn(
                name: "last_modified_by_id",
                table: "countries");

            migrationBuilder.DropColumn(
                name: "last_modified_on",
                table: "countries");

            migrationBuilder.DropColumn(
                name: "created_by_id",
                table: "city_scenarios");

            migrationBuilder.DropColumn(
                name: "last_modified_by_id",
                table: "city_scenarios");

            migrationBuilder.RenameColumn(
                name: "created_on",
                table: "country_profiles",
                newName: "last_updated_on");

            migrationBuilder.RenameColumn(
                name: "created_by_id",
                table: "country_profiles",
                newName: "last_updated_by_id");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "last_modified_on",
                table: "city_scenarios",
                type: "datetimeoffset",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)),
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetimeoffset",
                oldNullable: true);
        }
    }
}
