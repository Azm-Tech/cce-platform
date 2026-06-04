using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CCE.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DropStaleSoftDeleteColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "is_deleted", table: "policy_sections");
            migrationBuilder.DropColumn(name: "deleted_on", table: "policy_sections");
            migrationBuilder.DropColumn(name: "deleted_by_id", table: "policy_sections");

            migrationBuilder.DropColumn(name: "is_deleted", table: "knowledge_partners");
            migrationBuilder.DropColumn(name: "deleted_on", table: "knowledge_partners");
            migrationBuilder.DropColumn(name: "deleted_by_id", table: "knowledge_partners");

            migrationBuilder.DropColumn(name: "is_deleted", table: "glossary_entries");
            migrationBuilder.DropColumn(name: "deleted_on", table: "glossary_entries");
            migrationBuilder.DropColumn(name: "deleted_by_id", table: "glossary_entries");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                table: "policy_sections",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "deleted_on",
                table: "policy_sections",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "deleted_by_id",
                table: "policy_sections",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                table: "knowledge_partners",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "deleted_on",
                table: "knowledge_partners",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "deleted_by_id",
                table: "knowledge_partners",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                table: "glossary_entries",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "deleted_on",
                table: "glossary_entries",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "deleted_by_id",
                table: "glossary_entries",
                type: "uniqueidentifier",
                nullable: true);
        }
    }
}
