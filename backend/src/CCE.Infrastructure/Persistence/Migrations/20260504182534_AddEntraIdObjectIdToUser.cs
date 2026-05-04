using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CCE.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEntraIdObjectIdToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "entra_id_object_id",
                table: "AspNetUsers",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_asp_net_users_entra_id_object_id",
                table: "AspNetUsers",
                column: "entra_id_object_id",
                unique: true,
                filter: "[entra_id_object_id] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_asp_net_users_entra_id_object_id",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "entra_id_object_id",
                table: "AspNetUsers");
        }
    }
}
