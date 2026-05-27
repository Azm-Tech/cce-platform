using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CCE.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DropCountryCodeDialCodeUnique : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_country_code_dial_code_active",
                table: "country_codes");

            migrationBuilder.CreateIndex(
                name: "ix_country_code_dial_code",
                table: "country_codes",
                column: "dial_code");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_country_code_dial_code",
                table: "country_codes");

            migrationBuilder.CreateIndex(
                name: "ux_country_code_dial_code_active",
                table: "country_codes",
                column: "dial_code",
                unique: true,
                filter: "[is_deleted] = 0");
        }
    }
}
