using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CCE.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RefactorPlatformSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "deleted_by_id",
                table: "policy_sections");

            migrationBuilder.DropColumn(
                name: "deleted_on",
                table: "policy_sections");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                table: "policy_sections");

            migrationBuilder.DropColumn(
                name: "deleted_by_id",
                table: "knowledge_partners");

            migrationBuilder.DropColumn(
                name: "deleted_on",
                table: "knowledge_partners");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                table: "knowledge_partners");

            migrationBuilder.DropColumn(
                name: "deleted_by_id",
                table: "glossary_entries");

            migrationBuilder.DropColumn(
                name: "deleted_on",
                table: "glossary_entries");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                table: "glossary_entries");

            migrationBuilder.AddColumn<Guid>(
                name: "created_by_id",
                table: "homepage_countries",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "created_on",
                table: "homepage_countries",
                type: "datetimeoffset",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<Guid>(
                name: "last_modified_by_id",
                table: "homepage_countries",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "last_modified_on",
                table: "homepage_countries",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_policy_sections_policies_settings_id",
                table: "policy_sections",
                column: "policies_settings_id");

            migrationBuilder.CreateIndex(
                name: "ix_knowledge_partners_about_settings_id",
                table: "knowledge_partners",
                column: "about_settings_id");

            migrationBuilder.CreateIndex(
                name: "ix_glossary_entries_about_settings_id",
                table: "glossary_entries",
                column: "about_settings_id");

            migrationBuilder.AddForeignKey(
                name: "fk_glossary_entries_about_settings_about_settings_id",
                table: "glossary_entries",
                column: "about_settings_id",
                principalTable: "about_settings",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_homepage_countries_homepage_settings_homepage_settings_id",
                table: "homepage_countries",
                column: "homepage_settings_id",
                principalTable: "homepage_settings",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_knowledge_partners_about_settings_about_settings_id",
                table: "knowledge_partners",
                column: "about_settings_id",
                principalTable: "about_settings",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_policy_sections_policies_settings_policies_settings_id",
                table: "policy_sections",
                column: "policies_settings_id",
                principalTable: "policies_settings",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_glossary_entries_about_settings_about_settings_id",
                table: "glossary_entries");

            migrationBuilder.DropForeignKey(
                name: "fk_homepage_countries_homepage_settings_homepage_settings_id",
                table: "homepage_countries");

            migrationBuilder.DropForeignKey(
                name: "fk_knowledge_partners_about_settings_about_settings_id",
                table: "knowledge_partners");

            migrationBuilder.DropForeignKey(
                name: "fk_policy_sections_policies_settings_policies_settings_id",
                table: "policy_sections");

            migrationBuilder.DropIndex(
                name: "ix_policy_sections_policies_settings_id",
                table: "policy_sections");

            migrationBuilder.DropIndex(
                name: "ix_knowledge_partners_about_settings_id",
                table: "knowledge_partners");

            migrationBuilder.DropIndex(
                name: "ix_glossary_entries_about_settings_id",
                table: "glossary_entries");

            migrationBuilder.DropColumn(
                name: "created_by_id",
                table: "homepage_countries");

            migrationBuilder.DropColumn(
                name: "created_on",
                table: "homepage_countries");

            migrationBuilder.DropColumn(
                name: "last_modified_by_id",
                table: "homepage_countries");

            migrationBuilder.DropColumn(
                name: "last_modified_on",
                table: "homepage_countries");

            migrationBuilder.AddColumn<Guid>(
                name: "deleted_by_id",
                table: "policy_sections",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "deleted_on",
                table: "policy_sections",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                table: "policy_sections",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "deleted_by_id",
                table: "knowledge_partners",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "deleted_on",
                table: "knowledge_partners",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                table: "knowledge_partners",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "deleted_by_id",
                table: "glossary_entries",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "deleted_on",
                table: "glossary_entries",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                table: "glossary_entries",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
