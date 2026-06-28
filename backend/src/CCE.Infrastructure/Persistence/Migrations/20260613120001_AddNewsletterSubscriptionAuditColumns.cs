using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CCE.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddNewsletterSubscriptionAuditColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Idempotent: AddUserStatus (20260520) should have added these columns,
            // but they are missing from some DB instances. Use IF NOT EXISTS so this
            // migration is safe to run regardless of the current column state.
            migrationBuilder.Sql(@"
                IF NOT EXISTS (
                    SELECT 1 FROM sys.columns
                    WHERE object_id = OBJECT_ID(N'newsletter_subscriptions') AND name = N'created_by_id'
                )
                BEGIN
                    ALTER TABLE newsletter_subscriptions
                        ADD created_by_id uniqueidentifier NOT NULL
                            DEFAULT '00000000-0000-0000-0000-000000000000'
                END

                IF NOT EXISTS (
                    SELECT 1 FROM sys.columns
                    WHERE object_id = OBJECT_ID(N'newsletter_subscriptions') AND name = N'created_on'
                )
                BEGIN
                    ALTER TABLE newsletter_subscriptions
                        ADD created_on datetimeoffset NOT NULL
                            DEFAULT '0001-01-01 00:00:00 +00:00'
                END

                IF NOT EXISTS (
                    SELECT 1 FROM sys.columns
                    WHERE object_id = OBJECT_ID(N'newsletter_subscriptions') AND name = N'is_deleted'
                )
                BEGIN
                    ALTER TABLE newsletter_subscriptions
                        ADD is_deleted bit NOT NULL DEFAULT 0
                END

                IF NOT EXISTS (
                    SELECT 1 FROM sys.columns
                    WHERE object_id = OBJECT_ID(N'newsletter_subscriptions') AND name = N'deleted_by_id'
                )
                BEGIN
                    ALTER TABLE newsletter_subscriptions
                        ADD deleted_by_id uniqueidentifier NULL
                END

                IF NOT EXISTS (
                    SELECT 1 FROM sys.columns
                    WHERE object_id = OBJECT_ID(N'newsletter_subscriptions') AND name = N'deleted_on'
                )
                BEGIN
                    ALTER TABLE newsletter_subscriptions
                        ADD deleted_on datetimeoffset NULL
                END

                IF NOT EXISTS (
                    SELECT 1 FROM sys.columns
                    WHERE object_id = OBJECT_ID(N'newsletter_subscriptions') AND name = N'last_modified_by_id'
                )
                BEGIN
                    ALTER TABLE newsletter_subscriptions
                        ADD last_modified_by_id uniqueidentifier NULL
                END

                IF NOT EXISTS (
                    SELECT 1 FROM sys.columns
                    WHERE object_id = OBJECT_ID(N'newsletter_subscriptions') AND name = N'last_modified_on'
                )
                BEGIN
                    ALTER TABLE newsletter_subscriptions
                        ADD last_modified_on datetimeoffset NULL
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "created_by_id",      table: "newsletter_subscriptions");
            migrationBuilder.DropColumn(name: "created_on",         table: "newsletter_subscriptions");
            migrationBuilder.DropColumn(name: "is_deleted",         table: "newsletter_subscriptions");
            migrationBuilder.DropColumn(name: "deleted_by_id",      table: "newsletter_subscriptions");
            migrationBuilder.DropColumn(name: "deleted_on",         table: "newsletter_subscriptions");
            migrationBuilder.DropColumn(name: "last_modified_by_id", table: "newsletter_subscriptions");
            migrationBuilder.DropColumn(name: "last_modified_on",   table: "newsletter_subscriptions");
        }
    }
}
