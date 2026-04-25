using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CCE.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialAuditEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "audit_events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    occurred_on = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    actor = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    action = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    resource = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    correlation_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    diff = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_audit_events", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_audit_events_actor_occurred_on",
                table: "audit_events",
                columns: new[] { "actor", "occurred_on" });

            migrationBuilder.CreateIndex(
                name: "ix_audit_events_correlation_id",
                table: "audit_events",
                column: "correlation_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "audit_events");
        }
    }
}
