using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CCE.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddExpertRequestAttachments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "attachment_id",
                table: "expert_registration_requests");

            migrationBuilder.CreateTable(
                name: "expert_request_attachments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    expert_request_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    asset_file_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    attachment_type = table.Column<int>(type: "int", nullable: false),
                    uploaded_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_expert_request_attachments", x => x.id);
                    table.ForeignKey(
                        name: "fk_expert_request_attachments_expert_registration_requests_expert_request_id",
                        column: x => x.expert_request_id,
                        principalTable: "expert_registration_requests",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_expert_request_attachments_expert_request_id",
                table: "expert_request_attachments",
                column: "expert_request_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "expert_request_attachments");

            migrationBuilder.AddColumn<Guid>(
                name: "attachment_id",
                table: "expert_registration_requests",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));
        }
    }
}
