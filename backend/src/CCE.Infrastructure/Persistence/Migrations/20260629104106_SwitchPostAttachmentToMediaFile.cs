using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CCE.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SwitchPostAttachmentToMediaFile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── Data migration: copy existing asset_files referenced by post_attachments into media_files ──
            // Preserves the same IDs so the FK rename below works seamlessly.
            migrationBuilder.Sql("""
                INSERT INTO media_files (id, storage_key, url, original_file_name, mime_type, size_bytes,
                    title_ar, title_en, description_ar, description_en, alt_text_ar, alt_text_en,
                    uploaded_by_id, uploaded_on)
                SELECT af.id, af.url, af.url, af.original_file_name, af.mime_type, af.size_bytes,
                    NULL, NULL, NULL, NULL, NULL, NULL,
                    af.uploaded_by_id, af.uploaded_on
                FROM asset_files af
                WHERE af.id IN (SELECT DISTINCT pa.asset_file_id FROM post_attachments pa)
                  AND NOT EXISTS (SELECT 1 FROM media_files mf WHERE mf.id = af.id);
                """);

            migrationBuilder.DropForeignKey(
                name: "fk_post_attachments_asset_files_asset_file_id",
                table: "post_attachments");

            migrationBuilder.RenameColumn(
                name: "asset_file_id",
                table: "post_attachments",
                newName: "media_file_id");

            migrationBuilder.RenameIndex(
                name: "ix_post_attachments_asset_file_id",
                table: "post_attachments",
                newName: "ix_post_attachments_media_file_id");

            migrationBuilder.AddForeignKey(
                name: "fk_post_attachments_media_files_media_file_id",
                table: "post_attachments",
                column: "media_file_id",
                principalTable: "media_files",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_post_attachments_media_files_media_file_id",
                table: "post_attachments");

            migrationBuilder.RenameColumn(
                name: "media_file_id",
                table: "post_attachments",
                newName: "asset_file_id");

            migrationBuilder.RenameIndex(
                name: "ix_post_attachments_media_file_id",
                table: "post_attachments",
                newName: "ix_post_attachments_asset_file_id");

            migrationBuilder.AddForeignKey(
                name: "fk_post_attachments_asset_files_asset_file_id",
                table: "post_attachments",
                column: "asset_file_id",
                principalTable: "asset_files",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            // ── Data rollback: delete media_files that were copied by the Up migration ──
            // Only deletes rows that have no bilingual metadata (inserted by Up step).
            migrationBuilder.Sql("""
                DELETE FROM media_files
                WHERE title_ar IS NULL
                  AND title_en IS NULL
                  AND description_ar IS NULL
                  AND description_en IS NULL
                  AND alt_text_ar IS NULL
                  AND alt_text_en IS NULL
                  AND id IN (SELECT DISTINCT pa.asset_file_id FROM post_attachments pa);
                """);
        }
    }
}
