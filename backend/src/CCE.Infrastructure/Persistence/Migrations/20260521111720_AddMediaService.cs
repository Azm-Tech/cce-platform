using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CCE.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMediaService : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "media_files",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    storage_key = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    url = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
                    original_file_name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    mime_type = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    size_bytes = table.Column<long>(type: "bigint", nullable: false),
                    title_ar = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    title_en = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    description_ar = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    description_en = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    alt_text_ar = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    alt_text_en = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    uploaded_by_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    uploaded_on = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_media_files", x => x.id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "media_files");
        }
    }
}
