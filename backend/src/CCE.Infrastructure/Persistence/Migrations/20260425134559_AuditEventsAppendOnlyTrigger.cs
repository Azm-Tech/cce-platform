using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CCE.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AuditEventsAppendOnlyTrigger : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
CREATE TRIGGER trg_audit_events_no_update_delete
ON dbo.audit_events
INSTEAD OF UPDATE, DELETE
AS
BEGIN
    THROW 51000, 'audit_events is append-only; UPDATE and DELETE are not permitted.', 1;
END;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF OBJECT_ID('dbo.trg_audit_events_no_update_delete', 'TR') IS NOT NULL
    DROP TRIGGER dbo.trg_audit_events_no_update_delete;");
        }
    }
}
