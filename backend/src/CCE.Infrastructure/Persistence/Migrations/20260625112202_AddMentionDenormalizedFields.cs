using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CCE.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMentionDenormalizedFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "community_id",
                table: "mentions",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "post_id",
                table: "mentions",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "snippet",
                table: "mentions",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "ix_mention_community",
                table: "mentions",
                column: "community_id");

            migrationBuilder.CreateIndex(
                name: "ix_mention_post",
                table: "mentions",
                column: "post_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_mention_community",
                table: "mentions");

            migrationBuilder.DropIndex(
                name: "ix_mention_post",
                table: "mentions");

            migrationBuilder.DropColumn(
                name: "community_id",
                table: "mentions");

            migrationBuilder.DropColumn(
                name: "post_id",
                table: "mentions");

            migrationBuilder.DropColumn(
                name: "snippet",
                table: "mentions");
        }
    }
}
