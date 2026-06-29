using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CCE.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MakeMediaFileUrlStorageKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // MediaFile.Url now stores the storage key (not the full public URL).
            // Existing rows uploaded via UploadMediaCommandHandler have the full Supabase URL
            // in the url column — replace it with the storage_key which was already stored.
            migrationBuilder.Sql("""
                UPDATE media_files
                SET url = storage_key
                WHERE url <> storage_key;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Irreversible — we cannot reconstruct the original full public URL from the storage key
            // without knowing the base URL that was in use at upload time.
        }
    }
}
