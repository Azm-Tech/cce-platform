using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CCE.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DataDomainInitial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    normalized_name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    concurrency_stamp = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_asp_net_roles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    locale_preference = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: false),
                    knowledge_level = table.Column<int>(type: "int", nullable: false),
                    interests = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    country_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    avatar_url = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    user_name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    normalized_user_name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    normalized_email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    email_confirmed = table.Column<bool>(type: "bit", nullable: false),
                    password_hash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    security_stamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    concurrency_stamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    phone_number = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    phone_number_confirmed = table.Column<bool>(type: "bit", nullable: false),
                    two_factor_enabled = table.Column<bool>(type: "bit", nullable: false),
                    lockout_end = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    lockout_enabled = table.Column<bool>(type: "bit", nullable: false),
                    access_failed_count = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_asp_net_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "asset_files",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    url = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
                    original_file_name = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    size_bytes = table.Column<long>(type: "bigint", nullable: false),
                    mime_type = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    uploaded_by_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    uploaded_on = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    virus_scan_status = table.Column<int>(type: "int", nullable: false),
                    scanned_on = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_asset_files", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "city_scenario_results",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    scenario_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    computed_carbon_neutrality_year = table.Column<int>(type: "int", nullable: true),
                    computed_total_cost_usd = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    computed_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    engine_version = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_city_scenario_results", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "city_scenarios",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    name_ar = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    name_en = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    city_type = table.Column<int>(type: "int", nullable: false),
                    target_year = table.Column<int>(type: "int", nullable: false),
                    configuration_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    created_on = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    last_modified_on = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false),
                    deleted_on = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    deleted_by_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_city_scenarios", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "city_technologies",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    name_ar = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    name_en = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    description_ar = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    description_en = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    category_ar = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    category_en = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    carbon_impact_kg_per_year = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    cost_usd = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    icon_url = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    is_active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_city_technologies", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "countries",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    iso_alpha3 = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    iso_alpha2 = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: false),
                    name_ar = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    name_en = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    region_ar = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    region_en = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    flag_url = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
                    latest_kapsarc_snapshot_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false),
                    deleted_on = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    deleted_by_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_countries", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "country_kapsarc_snapshots",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    country_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    classification = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    performance_score = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    total_index = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    snapshot_taken_on = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    source_version = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_country_kapsarc_snapshots", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "country_profiles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    country_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    description_ar = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    description_en = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    key_initiatives_ar = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    key_initiatives_en = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    contact_info_ar = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    contact_info_en = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    last_updated_by_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    last_updated_on = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_country_profiles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "country_resource_requests",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    country_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    requested_by_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    status = table.Column<int>(type: "int", nullable: false),
                    proposed_title_ar = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    proposed_title_en = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    proposed_description_ar = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    proposed_description_en = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    proposed_resource_type = table.Column<int>(type: "int", nullable: false),
                    proposed_asset_file_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    submitted_on = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    admin_notes_ar = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    admin_notes_en = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    processed_by_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    processed_on = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false),
                    deleted_on = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    deleted_by_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_country_resource_requests", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    title_ar = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    title_en = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    description_ar = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    description_en = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    starts_on = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ends_on = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    location_ar = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    location_en = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    online_meeting_url = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    featured_image_url = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    i_cal_uid = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false),
                    deleted_on = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    deleted_by_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_events", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "expert_profiles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    bio_ar = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    bio_en = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    expertise_tags = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    academic_title_ar = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    academic_title_en = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    approved_on = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    approved_by_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false),
                    deleted_on = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    deleted_by_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_expert_profiles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "expert_registration_requests",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    requested_by_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    requested_bio_ar = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    requested_bio_en = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    requested_tags = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    submitted_on = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    status = table.Column<int>(type: "int", nullable: false),
                    processed_by_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    processed_on = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    rejection_reason_ar = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    rejection_reason_en = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false),
                    deleted_on = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    deleted_by_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_expert_registration_requests", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "homepage_sections",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    section_type = table.Column<int>(type: "int", nullable: false),
                    order_index = table.Column<int>(type: "int", nullable: false),
                    content_ar = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    content_en = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false),
                    deleted_on = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    deleted_by_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_homepage_sections", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "knowledge_map_associations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    node_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    associated_type = table.Column<int>(type: "int", nullable: false),
                    associated_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    order_index = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_knowledge_map_associations", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "knowledge_map_edges",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    map_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    from_node_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    to_node_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    relationship_type = table.Column<int>(type: "int", nullable: false),
                    order_index = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_knowledge_map_edges", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "knowledge_map_nodes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    map_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    name_ar = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    name_en = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    node_type = table.Column<int>(type: "int", nullable: false),
                    description_ar = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    description_en = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    icon_url = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    layout_x = table.Column<double>(type: "float", nullable: false),
                    layout_y = table.Column<double>(type: "float", nullable: false),
                    order_index = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_knowledge_map_nodes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "knowledge_maps",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    name_ar = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    name_en = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    description_ar = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    description_en = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    slug = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false),
                    deleted_on = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    deleted_by_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_knowledge_maps", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "news",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    title_ar = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    title_en = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    content_ar = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    content_en = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    slug = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    author_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    featured_image_url = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    published_on = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    is_featured = table.Column<bool>(type: "bit", nullable: false),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false),
                    deleted_on = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    deleted_by_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_news", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "newsletter_subscriptions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    email = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: false),
                    locale_preference = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: false),
                    is_confirmed = table.Column<bool>(type: "bit", nullable: false),
                    confirmation_token = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    confirmed_on = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    unsubscribed_on = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_newsletter_subscriptions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "notification_templates",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    code = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    subject_ar = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    subject_en = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    body_ar = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    body_en = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    channel = table.Column<int>(type: "int", nullable: false),
                    variable_schema_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_notification_templates", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "pages",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    slug = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    page_type = table.Column<int>(type: "int", nullable: false),
                    title_ar = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    title_en = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    content_ar = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    content_en = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false),
                    deleted_on = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    deleted_by_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_pages", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "post_follows",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    post_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    followed_on = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_post_follows", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "post_ratings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    post_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    stars = table.Column<int>(type: "int", nullable: false),
                    rated_on = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_post_ratings", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "post_replies",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    post_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    author_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    content = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: false),
                    locale = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: false),
                    parent_reply_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    is_by_expert = table.Column<bool>(type: "bit", nullable: false),
                    created_on = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false),
                    deleted_on = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    deleted_by_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_post_replies", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "posts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    topic_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    author_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    content = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: false),
                    locale = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: false),
                    is_answerable = table.Column<bool>(type: "bit", nullable: false),
                    answered_reply_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    created_on = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false),
                    deleted_on = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    deleted_by_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_posts", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "resource_categories",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    name_ar = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    name_en = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    slug = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    parent_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    order_index = table.Column<int>(type: "int", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_resource_categories", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "resources",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    title_ar = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    title_en = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    description_ar = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    description_en = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    resource_type = table.Column<int>(type: "int", nullable: false),
                    category_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    country_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    uploaded_by_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    asset_file_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    published_on = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    view_count = table.Column<long>(type: "bigint", nullable: false),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false),
                    deleted_on = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    deleted_by_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_resources", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "search_query_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    query_text = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    results_count = table.Column<int>(type: "int", nullable: false),
                    response_time_ms = table.Column<int>(type: "int", nullable: false),
                    locale = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: false),
                    submitted_on = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_search_query_logs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "service_ratings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    rating = table.Column<int>(type: "int", nullable: false),
                    comment_ar = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    comment_en = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    page = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    locale = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: false),
                    submitted_on = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_service_ratings", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "state_representative_assignments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    country_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    assigned_on = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    assigned_by_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    revoked_on = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    revoked_by_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false),
                    deleted_on = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    deleted_by_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_state_representative_assignments", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "topic_follows",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    topic_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    followed_on = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_topic_follows", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "topics",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    name_ar = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    name_en = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    description_ar = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    description_en = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    slug = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    parent_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    icon_url = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    order_index = table.Column<int>(type: "int", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false),
                    deleted_on = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    deleted_by_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_topics", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "user_follows",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    follower_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    followed_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    followed_on = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_follows", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "user_notifications",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    template_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    rendered_subject_ar = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    rendered_subject_en = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    rendered_body = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    rendered_locale = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: false),
                    channel = table.Column<int>(type: "int", nullable: false),
                    sent_on = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    read_on = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    status = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_notifications", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    role_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    claim_type = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    claim_value = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_asp_net_role_claims", x => x.id);
                    table.ForeignKey(
                        name: "fk_asp_net_role_claims_asp_net_roles_role_id",
                        column: x => x.role_id,
                        principalTable: "AspNetRoles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    claim_type = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    claim_value = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_asp_net_user_claims", x => x.id);
                    table.ForeignKey(
                        name: "fk_asp_net_user_claims_asp_net_users_user_id",
                        column: x => x.user_id,
                        principalTable: "AspNetUsers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    login_provider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    provider_key = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    provider_display_name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_asp_net_user_logins", x => new { x.login_provider, x.provider_key });
                    table.ForeignKey(
                        name: "fk_asp_net_user_logins_asp_net_users_user_id",
                        column: x => x.user_id,
                        principalTable: "AspNetUsers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    role_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_asp_net_user_roles", x => new { x.user_id, x.role_id });
                    table.ForeignKey(
                        name: "fk_asp_net_user_roles_asp_net_roles_role_id",
                        column: x => x.role_id,
                        principalTable: "AspNetRoles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_asp_net_user_roles_asp_net_users_user_id",
                        column: x => x.user_id,
                        principalTable: "AspNetUsers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    login_provider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    value = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_asp_net_user_tokens", x => new { x.user_id, x.login_provider, x.name });
                    table.ForeignKey(
                        name: "fk_asp_net_user_tokens_asp_net_users_user_id",
                        column: x => x.user_id,
                        principalTable: "AspNetUsers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_asp_net_role_claims_role_id",
                table: "AspNetRoleClaims",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "normalized_name",
                unique: true,
                filter: "[normalized_name] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_asp_net_user_claims_user_id",
                table: "AspNetUserClaims",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_asp_net_user_logins_user_id",
                table: "AspNetUserLogins",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_asp_net_user_roles_role_id",
                table: "AspNetUserRoles",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "normalized_email");

            migrationBuilder.CreateIndex(
                name: "ix_users_country_id",
                table: "AspNetUsers",
                column: "country_id");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "normalized_user_name",
                unique: true,
                filter: "[normalized_user_name] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_asset_file_scan_status",
                table: "asset_files",
                column: "virus_scan_status");

            migrationBuilder.CreateIndex(
                name: "ix_city_result_scenario_at",
                table: "city_scenario_results",
                columns: new[] { "scenario_id", "computed_at" });

            migrationBuilder.CreateIndex(
                name: "ix_city_scenario_user_modified",
                table: "city_scenarios",
                columns: new[] { "user_id", "last_modified_on" });

            migrationBuilder.CreateIndex(
                name: "ix_city_tech_is_active",
                table: "city_technologies",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_country_iso_alpha2",
                table: "countries",
                column: "iso_alpha2");

            migrationBuilder.CreateIndex(
                name: "ux_country_iso_alpha3_active",
                table: "countries",
                column: "iso_alpha3",
                unique: true,
                filter: "[is_deleted] = 0");

            migrationBuilder.CreateIndex(
                name: "ix_kapsarc_snapshot_country_taken",
                table: "country_kapsarc_snapshots",
                columns: new[] { "country_id", "snapshot_taken_on" });

            migrationBuilder.CreateIndex(
                name: "ux_country_profile_country_id",
                table: "country_profiles",
                column: "country_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_country_request_country_status",
                table: "country_resource_requests",
                columns: new[] { "country_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_event_starts_on",
                table: "events",
                column: "starts_on");

            migrationBuilder.CreateIndex(
                name: "ux_event_ical_uid",
                table: "events",
                column: "i_cal_uid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_expert_profile_active_user",
                table: "expert_profiles",
                column: "user_id",
                unique: true,
                filter: "[is_deleted] = 0");

            migrationBuilder.CreateIndex(
                name: "ix_expert_request_requested_by",
                table: "expert_registration_requests",
                column: "requested_by_id");

            migrationBuilder.CreateIndex(
                name: "ix_expert_request_status",
                table: "expert_registration_requests",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_homepage_section_active_order",
                table: "homepage_sections",
                columns: new[] { "is_active", "order_index" });

            migrationBuilder.CreateIndex(
                name: "ux_km_assoc_node_type_id",
                table: "knowledge_map_associations",
                columns: new[] { "node_id", "associated_type", "associated_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_km_edge_from_node",
                table: "knowledge_map_edges",
                column: "from_node_id");

            migrationBuilder.CreateIndex(
                name: "ix_km_edge_to_node",
                table: "knowledge_map_edges",
                column: "to_node_id");

            migrationBuilder.CreateIndex(
                name: "ux_km_edge_map_from_to_relation",
                table: "knowledge_map_edges",
                columns: new[] { "map_id", "from_node_id", "to_node_id", "relationship_type" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_km_node_map_order",
                table: "knowledge_map_nodes",
                columns: new[] { "map_id", "order_index" });

            migrationBuilder.CreateIndex(
                name: "ux_knowledge_map_slug_active",
                table: "knowledge_maps",
                column: "slug",
                unique: true,
                filter: "[is_deleted] = 0");

            migrationBuilder.CreateIndex(
                name: "ix_news_published_on",
                table: "news",
                column: "published_on");

            migrationBuilder.CreateIndex(
                name: "ux_news_slug_active",
                table: "news",
                column: "slug",
                unique: true,
                filter: "[is_deleted] = 0");

            migrationBuilder.CreateIndex(
                name: "ix_newsletter_token",
                table: "newsletter_subscriptions",
                column: "confirmation_token");

            migrationBuilder.CreateIndex(
                name: "ux_newsletter_email",
                table: "newsletter_subscriptions",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_notification_template_code",
                table: "notification_templates",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_page_type_slug_active",
                table: "pages",
                columns: new[] { "page_type", "slug" },
                unique: true,
                filter: "[is_deleted] = 0");

            migrationBuilder.CreateIndex(
                name: "ux_post_follow_post_user",
                table: "post_follows",
                columns: new[] { "post_id", "user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_post_rating_post_user",
                table: "post_ratings",
                columns: new[] { "post_id", "user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_post_reply_parent_id",
                table: "post_replies",
                column: "parent_reply_id");

            migrationBuilder.CreateIndex(
                name: "ix_post_reply_post_id",
                table: "post_replies",
                column: "post_id");

            migrationBuilder.CreateIndex(
                name: "ix_post_author_created",
                table: "posts",
                columns: new[] { "author_id", "created_on" });

            migrationBuilder.CreateIndex(
                name: "ix_post_topic_id",
                table: "posts",
                column: "topic_id");

            migrationBuilder.CreateIndex(
                name: "ix_resource_category_parent_id",
                table: "resource_categories",
                column: "parent_id");

            migrationBuilder.CreateIndex(
                name: "ux_resource_category_slug",
                table: "resource_categories",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_resource_asset_file_id",
                table: "resources",
                column: "asset_file_id");

            migrationBuilder.CreateIndex(
                name: "ix_resource_category_published",
                table: "resources",
                columns: new[] { "category_id", "published_on" });

            migrationBuilder.CreateIndex(
                name: "ix_resource_country_id",
                table: "resources",
                column: "country_id");

            migrationBuilder.CreateIndex(
                name: "ix_search_query_log_submitted_on",
                table: "search_query_logs",
                column: "submitted_on");

            migrationBuilder.CreateIndex(
                name: "ix_service_rating_submitted_on",
                table: "service_ratings",
                column: "submitted_on");

            migrationBuilder.CreateIndex(
                name: "ix_state_rep_country_id",
                table: "state_representative_assignments",
                column: "country_id");

            migrationBuilder.CreateIndex(
                name: "ix_state_rep_user_id",
                table: "state_representative_assignments",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ux_state_rep_active_user_country",
                table: "state_representative_assignments",
                columns: new[] { "user_id", "country_id" },
                unique: true,
                filter: "[is_deleted] = 0");

            migrationBuilder.CreateIndex(
                name: "ux_topic_follow_topic_user",
                table: "topic_follows",
                columns: new[] { "topic_id", "user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_topic_slug_active",
                table: "topics",
                column: "slug",
                unique: true,
                filter: "[is_deleted] = 0");

            migrationBuilder.CreateIndex(
                name: "ux_user_follow_follower_followed",
                table: "user_follows",
                columns: new[] { "follower_id", "followed_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_user_notification_user_status",
                table: "user_notifications",
                columns: new[] { "user_id", "status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "asset_files");

            migrationBuilder.DropTable(
                name: "city_scenario_results");

            migrationBuilder.DropTable(
                name: "city_scenarios");

            migrationBuilder.DropTable(
                name: "city_technologies");

            migrationBuilder.DropTable(
                name: "countries");

            migrationBuilder.DropTable(
                name: "country_kapsarc_snapshots");

            migrationBuilder.DropTable(
                name: "country_profiles");

            migrationBuilder.DropTable(
                name: "country_resource_requests");

            migrationBuilder.DropTable(
                name: "events");

            migrationBuilder.DropTable(
                name: "expert_profiles");

            migrationBuilder.DropTable(
                name: "expert_registration_requests");

            migrationBuilder.DropTable(
                name: "homepage_sections");

            migrationBuilder.DropTable(
                name: "knowledge_map_associations");

            migrationBuilder.DropTable(
                name: "knowledge_map_edges");

            migrationBuilder.DropTable(
                name: "knowledge_map_nodes");

            migrationBuilder.DropTable(
                name: "knowledge_maps");

            migrationBuilder.DropTable(
                name: "news");

            migrationBuilder.DropTable(
                name: "newsletter_subscriptions");

            migrationBuilder.DropTable(
                name: "notification_templates");

            migrationBuilder.DropTable(
                name: "pages");

            migrationBuilder.DropTable(
                name: "post_follows");

            migrationBuilder.DropTable(
                name: "post_ratings");

            migrationBuilder.DropTable(
                name: "post_replies");

            migrationBuilder.DropTable(
                name: "posts");

            migrationBuilder.DropTable(
                name: "resource_categories");

            migrationBuilder.DropTable(
                name: "resources");

            migrationBuilder.DropTable(
                name: "search_query_logs");

            migrationBuilder.DropTable(
                name: "service_ratings");

            migrationBuilder.DropTable(
                name: "state_representative_assignments");

            migrationBuilder.DropTable(
                name: "topic_follows");

            migrationBuilder.DropTable(
                name: "topics");

            migrationBuilder.DropTable(
                name: "user_follows");

            migrationBuilder.DropTable(
                name: "user_notifications");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "AspNetUsers");
        }
    }
}
