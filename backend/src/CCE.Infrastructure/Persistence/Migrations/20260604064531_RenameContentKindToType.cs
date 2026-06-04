using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CCE.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RenameContentKindToType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "kind",
                table: "country_content_requests",
                newName: "type");

            migrationBuilder.RenameIndex(
                name: "ix_country_content_request_country_status_kind",
                table: "country_content_requests",
                newName: "ix_country_content_request_country_status_type");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "type",
                table: "country_content_requests",
                newName: "kind");

            migrationBuilder.RenameIndex(
                name: "ix_country_content_request_country_status_type",
                table: "country_content_requests",
                newName: "ix_country_content_request_country_status_kind");
        }
    }
}
