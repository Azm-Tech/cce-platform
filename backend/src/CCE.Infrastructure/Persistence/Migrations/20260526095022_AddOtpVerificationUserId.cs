using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CCE.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOtpVerificationUserId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "EmailIndex",
                table: "AspNetUsers");

            migrationBuilder.AddColumn<Guid>(
                name: "user_id",
                table: "otp_verifications",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_otp_verifications_user_contact_type",
                table: "otp_verifications",
                columns: new[] { "user_id", "contact", "type_id" });

            migrationBuilder.CreateIndex(
                name: "ix_users_normalized_email_unique",
                table: "AspNetUsers",
                column: "normalized_email",
                unique: true,
                filter: "[normalized_email] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_otp_verifications_user_contact_type",
                table: "otp_verifications");

            migrationBuilder.DropIndex(
                name: "ix_users_normalized_email_unique",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "user_id",
                table: "otp_verifications");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "normalized_email");
        }
    }
}
