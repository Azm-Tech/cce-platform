using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CCE.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ExpandResourceTypeAndAddCountries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_resource_country_id",
                table: "resources");

            migrationBuilder.CreateTable(
                name: "resource_country",
                columns: table => new
                {
                    resource_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    country_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_resource_country", x => new { x.resource_id, x.country_id });
                    table.ForeignKey(
                        name: "fk_resource_country_resources_resource_id",
                        column: x => x.resource_id,
                        principalTable: "resources",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_resource_country_country_id",
                table: "resource_country",
                column: "country_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "resource_country");

            migrationBuilder.CreateIndex(
                name: "ix_resource_country_id",
                table: "resources",
                column: "country_id");
        }
    }
}
