using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CCE.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddContentInterestTopicLinks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "job_sector_id",
                table: "resources",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "knowledge_level_id",
                table: "resources",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "job_sector_id",
                table: "news",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "knowledge_level_id",
                table: "news",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "job_sector_id",
                table: "events",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "knowledge_level_id",
                table: "events",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "proposed_job_sector_id",
                table: "country_content_requests",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "proposed_knowledge_level_id",
                table: "country_content_requests",
                type: "uniqueidentifier",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "job_sector_id",
                table: "resources");

            migrationBuilder.DropColumn(
                name: "knowledge_level_id",
                table: "resources");

            migrationBuilder.DropColumn(
                name: "job_sector_id",
                table: "news");

            migrationBuilder.DropColumn(
                name: "knowledge_level_id",
                table: "news");

            migrationBuilder.DropColumn(
                name: "job_sector_id",
                table: "events");

            migrationBuilder.DropColumn(
                name: "knowledge_level_id",
                table: "events");

            migrationBuilder.DropColumn(
                name: "proposed_job_sector_id",
                table: "country_content_requests");

            migrationBuilder.DropColumn(
                name: "proposed_knowledge_level_id",
                table: "country_content_requests");
        }
    }
}
