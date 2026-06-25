using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CCE.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUserDeviceToken : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "user_device_token",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    device_id = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    token = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    platform = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    registered_on = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    last_seen_on = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_device_token", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_user_device_token_token",
                table: "user_device_token",
                column: "token");

            migrationBuilder.CreateIndex(
                name: "ix_user_device_token_user_id_device_id",
                table: "user_device_token",
                columns: new[] { "user_id", "device_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_user_device_token_user_id_is_active",
                table: "user_device_token",
                columns: new[] { "user_id", "is_active" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "user_device_token");
        }
    }
}
