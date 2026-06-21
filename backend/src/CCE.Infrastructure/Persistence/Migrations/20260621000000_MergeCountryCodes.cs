using CCE.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CCE.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(CceDbContext))]
    [Migration("20260621000000_MergeCountryCodes")]
    public partial class MergeCountryCodes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── 1. Relax NOT NULL on CCE-country-only columns ──────────────────────
            migrationBuilder.AlterColumn<string>(
                name: "iso_alpha3",
                table: "countries",
                type: "nvarchar(3)",
                maxLength: 3,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(3)",
                oldMaxLength: 3);

            migrationBuilder.AlterColumn<string>(
                name: "iso_alpha2",
                table: "countries",
                type: "nvarchar(2)",
                maxLength: 2,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(2)",
                oldMaxLength: 2);

            migrationBuilder.AlterColumn<string>(
                name: "region_ar",
                table: "countries",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(128)",
                oldMaxLength: 128);

            migrationBuilder.AlterColumn<string>(
                name: "region_en",
                table: "countries",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(128)",
                oldMaxLength: 128);

            // ── 2. Add new columns to countries ───────────────────────────────────
            migrationBuilder.AddColumn<string>(
                name: "dial_code",
                table: "countries",
                type: "nvarchar(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_cce_country",
                table: "countries",
                type: "bit",
                nullable: false,
                defaultValue: false);

            // ── 3. Swap the unique index BEFORE inserting world countries ─────────
            // The old index (filter: is_deleted=0) treats multiple NULLs as duplicates.
            // The new index (filter: is_deleted=0 AND is_cce_country=1) excludes lookup rows entirely.
            migrationBuilder.DropIndex(
                name: "ux_country_iso_alpha3_active",
                table: "countries");

            migrationBuilder.CreateIndex(
                name: "ux_country_iso_alpha3_active",
                table: "countries",
                column: "iso_alpha3",
                unique: true,
                filter: "[is_deleted] = 0 AND [is_cce_country] = 1");

            // ── 4. Data migration ──────────────────────────────────────────────────
            // 4a. All rows that existed before this migration are CCE countries — mark them.
            migrationBuilder.Sql("UPDATE countries SET is_cce_country = 1;");

            // 4b. Populate dial_code on existing CCE countries by name-matching.
            migrationBuilder.Sql(@"
UPDATE c
SET c.dial_code = cc.dial_code
FROM countries c
INNER JOIN country_codes cc
    ON cc.name_en = c.name_en OR cc.name_ar = c.name_ar
WHERE c.is_cce_country = 1;
");

            // 4c. Insert unmatched country_codes entries as lookup rows (is_cce_country = 0).
            //     Reuse the same GUID so users.country_code_id can be migrated directly.
            migrationBuilder.Sql(@"
INSERT INTO countries
    (id, name_ar, name_en, flag_url, dial_code, is_cce_country,
     is_active, is_deleted, created_by_id, created_on,
     iso_alpha2, iso_alpha3, region_ar, region_en,
     latest_kapsarc_snapshot_id, last_modified_by_id, last_modified_on, deleted_by_id, deleted_on)
SELECT
    cc.id,
    cc.name_ar,
    cc.name_en,
    ISNULL(cc.flag_url, ''),
    cc.dial_code,
    0,            -- is_cce_country = false
    cc.is_active,
    cc.is_deleted,
    cc.created_by_id,
    cc.created_on,
    NULL, NULL, NULL, NULL,   -- iso_alpha2/3, region_ar/en
    NULL, cc.last_modified_by_id, cc.last_modified_on, cc.deleted_by_id, cc.deleted_on
FROM country_codes cc
WHERE NOT EXISTS (
    SELECT 1 FROM countries c
    WHERE c.name_en = cc.name_en OR c.name_ar = cc.name_ar
);
");

            // 4d. Migrate users: copy country_code_id → country_id where country_id is not yet set.
            migrationBuilder.Sql(@"
-- Users whose dial-code country happens to be a CCE member → point to the CCE country row
UPDATE u
SET u.country_id = c.id
FROM [AspNetUsers] u
INNER JOIN country_codes cc ON cc.id = u.country_code_id
INNER JOIN countries c ON (c.name_en = cc.name_en OR c.name_ar = cc.name_ar) AND c.is_cce_country = 1
WHERE u.country_code_id IS NOT NULL AND u.country_id IS NULL;

-- Remaining users → their country_code id is now a countries row (inserted in step 4c)
UPDATE [AspNetUsers]
SET country_id = country_code_id
WHERE country_code_id IS NOT NULL AND country_id IS NULL;
");

            // ── 5. Drop country_code_id from users ────────────────────────────────
            migrationBuilder.DropIndex(
                name: "ix_users_country_code_id",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "country_code_id",
                table: "AspNetUsers");

            // ── 6. Drop the country_codes table ───────────────────────────────────
            migrationBuilder.DropTable(
                name: "country_codes");

            // ── 7. Add filtered index for dial_code lookups ───────────────────────
            migrationBuilder.CreateIndex(
                name: "ix_country_dial_code",
                table: "countries",
                column: "dial_code",
                filter: "[dial_code] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // ── Restore country_codes table ───────────────────────────────────────
            migrationBuilder.CreateTable(
                name: "country_codes",
                columns: table => new
                {
                    id = table.Column<System.Guid>(type: "uniqueidentifier", nullable: false),
                    created_by_id = table.Column<System.Guid>(type: "uniqueidentifier", nullable: false),
                    created_on = table.Column<System.DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    deleted_by_id = table.Column<System.Guid>(type: "uniqueidentifier", nullable: true),
                    deleted_on = table.Column<System.DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    dial_code = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    flag_url = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false),
                    last_modified_by_id = table.Column<System.Guid>(type: "uniqueidentifier", nullable: true),
                    last_modified_on = table.Column<System.DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    name_ar = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    name_en = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_country_codes", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_country_code_dial_code",
                table: "country_codes",
                column: "dial_code");

            // ── Restore users.country_code_id ─────────────────────────────────────
            migrationBuilder.AddColumn<System.Guid>(
                name: "country_code_id",
                table: "AspNetUsers",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_users_country_code_id",
                table: "AspNetUsers",
                column: "country_code_id");

            // ── Restore countries indexes ─────────────────────────────────────────
            migrationBuilder.DropIndex(
                name: "ix_country_dial_code",
                table: "countries");

            migrationBuilder.DropIndex(
                name: "ux_country_iso_alpha3_active",
                table: "countries");

            migrationBuilder.CreateIndex(
                name: "ux_country_iso_alpha3_active",
                table: "countries",
                column: "iso_alpha3",
                unique: true,
                filter: "[is_deleted] = 0");

            // ── Drop new countries columns ─────────────────────────────────────────
            migrationBuilder.DropColumn(
                name: "dial_code",
                table: "countries");

            migrationBuilder.DropColumn(
                name: "is_cce_country",
                table: "countries");

            // ── Restore NOT NULL constraints ──────────────────────────────────────
            migrationBuilder.AlterColumn<string>(
                name: "iso_alpha3",
                table: "countries",
                type: "nvarchar(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(3)",
                oldMaxLength: 3,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "iso_alpha2",
                table: "countries",
                type: "nvarchar(2)",
                maxLength: 2,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(2)",
                oldMaxLength: 2,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "region_ar",
                table: "countries",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(128)",
                oldMaxLength: 128,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "region_en",
                table: "countries",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(128)",
                oldMaxLength: 128,
                oldNullable: true);
        }
    }
}
