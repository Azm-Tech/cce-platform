using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CCE.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddNewsIdToNewsFollowLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM news_follow_logs");

            migrationBuilder.DropColumn(
                name: "status",
                table: "news_follow_logs");

            migrationBuilder.AddColumn<Guid>(
                name: "news_id",
                table: "news_follow_logs",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "ix_news_follow_log_user_news",
                table: "news_follow_logs",
                columns: new[] { "user_id", "news_id" });

            migrationBuilder.CreateIndex(
                name: "ix_news_follow_logs_news_id",
                table: "news_follow_logs",
                column: "news_id");

            migrationBuilder.AddForeignKey(
                name: "fk_news_follow_logs_news_news_id",
                table: "news_follow_logs",
                column: "news_id",
                principalTable: "news",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_news_follow_logs_news_news_id",
                table: "news_follow_logs");

            migrationBuilder.DropIndex(
                name: "ix_news_follow_log_user_news",
                table: "news_follow_logs");

            migrationBuilder.DropIndex(
                name: "ix_news_follow_logs_news_id",
                table: "news_follow_logs");

            migrationBuilder.DropColumn(
                name: "news_id",
                table: "news_follow_logs");

            migrationBuilder.AddColumn<int>(
                name: "status",
                table: "news_follow_logs",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
