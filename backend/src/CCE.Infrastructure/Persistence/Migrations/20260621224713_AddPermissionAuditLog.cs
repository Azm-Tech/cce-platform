using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CCE.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPermissionAuditLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "permission_audit_logs",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    changed_at_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    changed_by_user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    changed_by_email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    role_name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    permission_name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    action = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_permission_audit_logs", x => x.id);
                });

            migrationBuilder.Sql(
                "UPDATE AspNetRoleClaims SET claim_value = LOWER(claim_value) WHERE claim_type = 'permission'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "permission_audit_logs");
        }
    }
}
