using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CCE.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddServiceEvaluation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "service_evaluations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    overall_satisfaction = table.Column<int>(type: "int", nullable: false),
                    ease_of_use = table.Column<int>(type: "int", nullable: false),
                    content_suitability = table.Column<int>(type: "int", nullable: false),
                    feedback = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    created_on = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    created_by_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    last_modified_on = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    last_modified_by_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_service_evaluations", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_service_evaluation_created_on",
                table: "service_evaluations",
                column: "created_on");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "service_evaluations");
        }
    }
}
