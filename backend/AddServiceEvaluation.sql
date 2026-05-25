IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [migration_id] nvarchar(150) NOT NULL,
        [product_version] nvarchar(32) NOT NULL,
        CONSTRAINT [pk___ef_migrations_history] PRIMARY KEY ([migration_id])
    );
END;
GO

BEGIN TRANSACTION;
CREATE TABLE [audit_events] (
    [id] uniqueidentifier NOT NULL,
    [occurred_on] datetimeoffset NOT NULL,
    [actor] nvarchar(256) NOT NULL,
    [action] nvarchar(128) NOT NULL,
    [resource] nvarchar(512) NOT NULL,
    [correlation_id] uniqueidentifier NOT NULL,
    [diff] nvarchar(max) NULL,
    CONSTRAINT [pk_audit_events] PRIMARY KEY ([id])
);

CREATE INDEX [ix_audit_events_actor_occurred_on] ON [audit_events] ([actor], [occurred_on]);

CREATE INDEX [ix_audit_events_correlation_id] ON [audit_events] ([correlation_id]);

INSERT INTO [__EFMigrationsHistory] ([migration_id], [product_version])
VALUES (N'20260425134009_InitialAuditEvents', N'10.0.1');

COMMIT;
GO

BEGIN TRANSACTION;

CREATE TRIGGER trg_audit_events_no_update_delete
ON dbo.audit_events
INSTEAD OF UPDATE, DELETE
AS
BEGIN
    THROW 51000, 'audit_events is append-only; UPDATE and DELETE are not permitted.', 1;
END;

INSERT INTO [__EFMigrationsHistory] ([migration_id], [product_version])
VALUES (N'20260425134559_AuditEventsAppendOnlyTrigger', N'10.0.1');

COMMIT;
GO

BEGIN TRANSACTION;
CREATE TABLE [AspNetRoles] (
    [id] uniqueidentifier NOT NULL,
    [name] nvarchar(256) NULL,
    [normalized_name] nvarchar(256) NULL,
    [concurrency_stamp] nvarchar(max) NULL,
    CONSTRAINT [pk_asp_net_roles] PRIMARY KEY ([id])
);

CREATE TABLE [AspNetUsers] (
    [id] uniqueidentifier NOT NULL,
    [locale_preference] nvarchar(2) NOT NULL,
    [knowledge_level] int NOT NULL,
    [interests] nvarchar(max) NOT NULL,
    [country_id] uniqueidentifier NULL,
    [avatar_url] nvarchar(2048) NULL,
    [user_name] nvarchar(256) NULL,
    [normalized_user_name] nvarchar(256) NULL,
    [email] nvarchar(256) NULL,
    [normalized_email] nvarchar(256) NULL,
    [email_confirmed] bit NOT NULL,
    [password_hash] nvarchar(max) NULL,
    [security_stamp] nvarchar(max) NULL,
    [concurrency_stamp] nvarchar(max) NULL,
    [phone_number] nvarchar(max) NULL,
    [phone_number_confirmed] bit NOT NULL,
    [two_factor_enabled] bit NOT NULL,
    [lockout_end] datetimeoffset NULL,
    [lockout_enabled] bit NOT NULL,
    [access_failed_count] int NOT NULL,
    CONSTRAINT [pk_asp_net_users] PRIMARY KEY ([id])
);

CREATE TABLE [asset_files] (
    [id] uniqueidentifier NOT NULL,
    [url] nvarchar(2048) NOT NULL,
    [original_file_name] nvarchar(512) NOT NULL,
    [size_bytes] bigint NOT NULL,
    [mime_type] nvarchar(128) NOT NULL,
    [uploaded_by_id] uniqueidentifier NOT NULL,
    [uploaded_on] datetimeoffset NOT NULL,
    [virus_scan_status] int NOT NULL,
    [scanned_on] datetimeoffset NULL,
    CONSTRAINT [pk_asset_files] PRIMARY KEY ([id])
);

CREATE TABLE [city_scenario_results] (
    [id] uniqueidentifier NOT NULL,
    [scenario_id] uniqueidentifier NOT NULL,
    [computed_carbon_neutrality_year] int NULL,
    [computed_total_cost_usd] decimal(18,2) NOT NULL,
    [computed_at] datetimeoffset NOT NULL,
    [engine_version] nvarchar(64) NOT NULL,
    CONSTRAINT [pk_city_scenario_results] PRIMARY KEY ([id])
);

CREATE TABLE [city_scenarios] (
    [id] uniqueidentifier NOT NULL,
    [user_id] uniqueidentifier NOT NULL,
    [name_ar] nvarchar(256) NOT NULL,
    [name_en] nvarchar(256) NOT NULL,
    [city_type] int NOT NULL,
    [target_year] int NOT NULL,
    [configuration_json] nvarchar(max) NOT NULL,
    [created_on] datetimeoffset NOT NULL,
    [last_modified_on] datetimeoffset NOT NULL,
    [is_deleted] bit NOT NULL,
    [deleted_on] datetimeoffset NULL,
    [deleted_by_id] uniqueidentifier NULL,
    CONSTRAINT [pk_city_scenarios] PRIMARY KEY ([id])
);

CREATE TABLE [city_technologies] (
    [id] uniqueidentifier NOT NULL,
    [name_ar] nvarchar(256) NOT NULL,
    [name_en] nvarchar(256) NOT NULL,
    [description_ar] nvarchar(max) NOT NULL,
    [description_en] nvarchar(max) NOT NULL,
    [category_ar] nvarchar(128) NOT NULL,
    [category_en] nvarchar(128) NOT NULL,
    [carbon_impact_kg_per_year] decimal(18,2) NOT NULL,
    [cost_usd] decimal(18,2) NOT NULL,
    [icon_url] nvarchar(2048) NULL,
    [is_active] bit NOT NULL,
    CONSTRAINT [pk_city_technologies] PRIMARY KEY ([id])
);

CREATE TABLE [countries] (
    [id] uniqueidentifier NOT NULL,
    [iso_alpha3] nvarchar(3) NOT NULL,
    [iso_alpha2] nvarchar(2) NOT NULL,
    [name_ar] nvarchar(256) NOT NULL,
    [name_en] nvarchar(256) NOT NULL,
    [region_ar] nvarchar(128) NOT NULL,
    [region_en] nvarchar(128) NOT NULL,
    [flag_url] nvarchar(2048) NOT NULL,
    [latest_kapsarc_snapshot_id] uniqueidentifier NULL,
    [is_active] bit NOT NULL,
    [is_deleted] bit NOT NULL,
    [deleted_on] datetimeoffset NULL,
    [deleted_by_id] uniqueidentifier NULL,
    CONSTRAINT [pk_countries] PRIMARY KEY ([id])
);

CREATE TABLE [country_kapsarc_snapshots] (
    [id] uniqueidentifier NOT NULL,
    [country_id] uniqueidentifier NOT NULL,
    [classification] nvarchar(64) NOT NULL,
    [performance_score] decimal(5,2) NOT NULL,
    [total_index] decimal(5,2) NOT NULL,
    [snapshot_taken_on] datetimeoffset NOT NULL,
    [source_version] nvarchar(32) NULL,
    CONSTRAINT [pk_country_kapsarc_snapshots] PRIMARY KEY ([id])
);

CREATE TABLE [country_profiles] (
    [id] uniqueidentifier NOT NULL,
    [country_id] uniqueidentifier NOT NULL,
    [description_ar] nvarchar(max) NOT NULL,
    [description_en] nvarchar(max) NOT NULL,
    [key_initiatives_ar] nvarchar(max) NOT NULL,
    [key_initiatives_en] nvarchar(max) NOT NULL,
    [contact_info_ar] nvarchar(2000) NULL,
    [contact_info_en] nvarchar(2000) NULL,
    [last_updated_by_id] uniqueidentifier NOT NULL,
    [last_updated_on] datetimeoffset NOT NULL,
    [row_version] rowversion NOT NULL,
    CONSTRAINT [pk_country_profiles] PRIMARY KEY ([id])
);

CREATE TABLE [country_resource_requests] (
    [id] uniqueidentifier NOT NULL,
    [country_id] uniqueidentifier NOT NULL,
    [requested_by_id] uniqueidentifier NOT NULL,
    [status] int NOT NULL,
    [proposed_title_ar] nvarchar(512) NOT NULL,
    [proposed_title_en] nvarchar(512) NOT NULL,
    [proposed_description_ar] nvarchar(max) NOT NULL,
    [proposed_description_en] nvarchar(max) NOT NULL,
    [proposed_resource_type] int NOT NULL,
    [proposed_asset_file_id] uniqueidentifier NOT NULL,
    [submitted_on] datetimeoffset NOT NULL,
    [admin_notes_ar] nvarchar(2000) NULL,
    [admin_notes_en] nvarchar(2000) NULL,
    [processed_by_id] uniqueidentifier NULL,
    [processed_on] datetimeoffset NULL,
    [is_deleted] bit NOT NULL,
    [deleted_on] datetimeoffset NULL,
    [deleted_by_id] uniqueidentifier NULL,
    CONSTRAINT [pk_country_resource_requests] PRIMARY KEY ([id])
);

CREATE TABLE [events] (
    [id] uniqueidentifier NOT NULL,
    [title_ar] nvarchar(512) NOT NULL,
    [title_en] nvarchar(512) NOT NULL,
    [description_ar] nvarchar(max) NOT NULL,
    [description_en] nvarchar(max) NOT NULL,
    [starts_on] datetimeoffset NOT NULL,
    [ends_on] datetimeoffset NOT NULL,
    [location_ar] nvarchar(512) NULL,
    [location_en] nvarchar(512) NULL,
    [online_meeting_url] nvarchar(2048) NULL,
    [featured_image_url] nvarchar(2048) NULL,
    [i_cal_uid] nvarchar(256) NOT NULL,
    [row_version] rowversion NOT NULL,
    [is_deleted] bit NOT NULL,
    [deleted_on] datetimeoffset NULL,
    [deleted_by_id] uniqueidentifier NULL,
    CONSTRAINT [pk_events] PRIMARY KEY ([id])
);

CREATE TABLE [expert_profiles] (
    [id] uniqueidentifier NOT NULL,
    [user_id] uniqueidentifier NOT NULL,
    [bio_ar] nvarchar(2000) NOT NULL,
    [bio_en] nvarchar(2000) NOT NULL,
    [expertise_tags] nvarchar(max) NOT NULL,
    [academic_title_ar] nvarchar(128) NOT NULL,
    [academic_title_en] nvarchar(128) NOT NULL,
    [approved_on] datetimeoffset NOT NULL,
    [approved_by_id] uniqueidentifier NOT NULL,
    [is_deleted] bit NOT NULL,
    [deleted_on] datetimeoffset NULL,
    [deleted_by_id] uniqueidentifier NULL,
    CONSTRAINT [pk_expert_profiles] PRIMARY KEY ([id])
);

CREATE TABLE [expert_registration_requests] (
    [id] uniqueidentifier NOT NULL,
    [requested_by_id] uniqueidentifier NOT NULL,
    [requested_bio_ar] nvarchar(2000) NOT NULL,
    [requested_bio_en] nvarchar(2000) NOT NULL,
    [requested_tags] nvarchar(max) NOT NULL,
    [submitted_on] datetimeoffset NOT NULL,
    [status] int NOT NULL,
    [processed_by_id] uniqueidentifier NULL,
    [processed_on] datetimeoffset NULL,
    [rejection_reason_ar] nvarchar(1000) NULL,
    [rejection_reason_en] nvarchar(1000) NULL,
    [is_deleted] bit NOT NULL,
    [deleted_on] datetimeoffset NULL,
    [deleted_by_id] uniqueidentifier NULL,
    CONSTRAINT [pk_expert_registration_requests] PRIMARY KEY ([id])
);

CREATE TABLE [homepage_sections] (
    [id] uniqueidentifier NOT NULL,
    [section_type] int NOT NULL,
    [order_index] int NOT NULL,
    [content_ar] nvarchar(max) NOT NULL,
    [content_en] nvarchar(max) NOT NULL,
    [is_active] bit NOT NULL,
    [is_deleted] bit NOT NULL,
    [deleted_on] datetimeoffset NULL,
    [deleted_by_id] uniqueidentifier NULL,
    CONSTRAINT [pk_homepage_sections] PRIMARY KEY ([id])
);

CREATE TABLE [knowledge_map_associations] (
    [id] uniqueidentifier NOT NULL,
    [node_id] uniqueidentifier NOT NULL,
    [associated_type] int NOT NULL,
    [associated_id] uniqueidentifier NOT NULL,
    [order_index] int NOT NULL,
    CONSTRAINT [pk_knowledge_map_associations] PRIMARY KEY ([id])
);

CREATE TABLE [knowledge_map_edges] (
    [id] uniqueidentifier NOT NULL,
    [map_id] uniqueidentifier NOT NULL,
    [from_node_id] uniqueidentifier NOT NULL,
    [to_node_id] uniqueidentifier NOT NULL,
    [relationship_type] int NOT NULL,
    [order_index] int NOT NULL,
    CONSTRAINT [pk_knowledge_map_edges] PRIMARY KEY ([id])
);

CREATE TABLE [knowledge_map_nodes] (
    [id] uniqueidentifier NOT NULL,
    [map_id] uniqueidentifier NOT NULL,
    [name_ar] nvarchar(256) NOT NULL,
    [name_en] nvarchar(256) NOT NULL,
    [node_type] int NOT NULL,
    [description_ar] nvarchar(max) NULL,
    [description_en] nvarchar(max) NULL,
    [icon_url] nvarchar(2048) NULL,
    [layout_x] float NOT NULL,
    [layout_y] float NOT NULL,
    [order_index] int NOT NULL,
    CONSTRAINT [pk_knowledge_map_nodes] PRIMARY KEY ([id])
);

CREATE TABLE [knowledge_maps] (
    [id] uniqueidentifier NOT NULL,
    [name_ar] nvarchar(256) NOT NULL,
    [name_en] nvarchar(256) NOT NULL,
    [description_ar] nvarchar(max) NOT NULL,
    [description_en] nvarchar(max) NOT NULL,
    [slug] nvarchar(128) NOT NULL,
    [is_active] bit NOT NULL,
    [row_version] rowversion NOT NULL,
    [is_deleted] bit NOT NULL,
    [deleted_on] datetimeoffset NULL,
    [deleted_by_id] uniqueidentifier NULL,
    CONSTRAINT [pk_knowledge_maps] PRIMARY KEY ([id])
);

CREATE TABLE [news] (
    [id] uniqueidentifier NOT NULL,
    [title_ar] nvarchar(512) NOT NULL,
    [title_en] nvarchar(512) NOT NULL,
    [content_ar] nvarchar(max) NOT NULL,
    [content_en] nvarchar(max) NOT NULL,
    [slug] nvarchar(256) NOT NULL,
    [author_id] uniqueidentifier NOT NULL,
    [featured_image_url] nvarchar(2048) NULL,
    [published_on] datetimeoffset NULL,
    [is_featured] bit NOT NULL,
    [row_version] rowversion NOT NULL,
    [is_deleted] bit NOT NULL,
    [deleted_on] datetimeoffset NULL,
    [deleted_by_id] uniqueidentifier NULL,
    CONSTRAINT [pk_news] PRIMARY KEY ([id])
);

CREATE TABLE [newsletter_subscriptions] (
    [id] uniqueidentifier NOT NULL,
    [email] nvarchar(320) NOT NULL,
    [locale_preference] nvarchar(2) NOT NULL,
    [is_confirmed] bit NOT NULL,
    [confirmation_token] nvarchar(64) NOT NULL,
    [confirmed_on] datetimeoffset NULL,
    [unsubscribed_on] datetimeoffset NULL,
    CONSTRAINT [pk_newsletter_subscriptions] PRIMARY KEY ([id])
);

CREATE TABLE [notification_templates] (
    [id] uniqueidentifier NOT NULL,
    [code] nvarchar(64) NOT NULL,
    [subject_ar] nvarchar(512) NOT NULL,
    [subject_en] nvarchar(512) NOT NULL,
    [body_ar] nvarchar(max) NOT NULL,
    [body_en] nvarchar(max) NOT NULL,
    [channel] int NOT NULL,
    [variable_schema_json] nvarchar(max) NOT NULL,
    [is_active] bit NOT NULL,
    CONSTRAINT [pk_notification_templates] PRIMARY KEY ([id])
);

CREATE TABLE [pages] (
    [id] uniqueidentifier NOT NULL,
    [slug] nvarchar(256) NOT NULL,
    [page_type] int NOT NULL,
    [title_ar] nvarchar(512) NOT NULL,
    [title_en] nvarchar(512) NOT NULL,
    [content_ar] nvarchar(max) NOT NULL,
    [content_en] nvarchar(max) NOT NULL,
    [row_version] rowversion NOT NULL,
    [is_deleted] bit NOT NULL,
    [deleted_on] datetimeoffset NULL,
    [deleted_by_id] uniqueidentifier NULL,
    CONSTRAINT [pk_pages] PRIMARY KEY ([id])
);

CREATE TABLE [post_follows] (
    [id] uniqueidentifier NOT NULL,
    [post_id] uniqueidentifier NOT NULL,
    [user_id] uniqueidentifier NOT NULL,
    [followed_on] datetimeoffset NOT NULL,
    CONSTRAINT [pk_post_follows] PRIMARY KEY ([id])
);

CREATE TABLE [post_ratings] (
    [id] uniqueidentifier NOT NULL,
    [post_id] uniqueidentifier NOT NULL,
    [user_id] uniqueidentifier NOT NULL,
    [stars] int NOT NULL,
    [rated_on] datetimeoffset NOT NULL,
    CONSTRAINT [pk_post_ratings] PRIMARY KEY ([id])
);

CREATE TABLE [post_replies] (
    [id] uniqueidentifier NOT NULL,
    [post_id] uniqueidentifier NOT NULL,
    [author_id] uniqueidentifier NOT NULL,
    [content] nvarchar(max) NOT NULL,
    [locale] nvarchar(2) NOT NULL,
    [parent_reply_id] uniqueidentifier NULL,
    [is_by_expert] bit NOT NULL,
    [created_on] datetimeoffset NOT NULL,
    [is_deleted] bit NOT NULL,
    [deleted_on] datetimeoffset NULL,
    [deleted_by_id] uniqueidentifier NULL,
    CONSTRAINT [pk_post_replies] PRIMARY KEY ([id])
);

CREATE TABLE [posts] (
    [id] uniqueidentifier NOT NULL,
    [topic_id] uniqueidentifier NOT NULL,
    [author_id] uniqueidentifier NOT NULL,
    [content] nvarchar(max) NOT NULL,
    [locale] nvarchar(2) NOT NULL,
    [is_answerable] bit NOT NULL,
    [answered_reply_id] uniqueidentifier NULL,
    [created_on] datetimeoffset NOT NULL,
    [is_deleted] bit NOT NULL,
    [deleted_on] datetimeoffset NULL,
    [deleted_by_id] uniqueidentifier NULL,
    CONSTRAINT [pk_posts] PRIMARY KEY ([id])
);

CREATE TABLE [resource_categories] (
    [id] uniqueidentifier NOT NULL,
    [name_ar] nvarchar(256) NOT NULL,
    [name_en] nvarchar(256) NOT NULL,
    [slug] nvarchar(128) NOT NULL,
    [parent_id] uniqueidentifier NULL,
    [order_index] int NOT NULL,
    [is_active] bit NOT NULL,
    CONSTRAINT [pk_resource_categories] PRIMARY KEY ([id])
);

CREATE TABLE [resources] (
    [id] uniqueidentifier NOT NULL,
    [title_ar] nvarchar(512) NOT NULL,
    [title_en] nvarchar(512) NOT NULL,
    [description_ar] nvarchar(max) NOT NULL,
    [description_en] nvarchar(max) NOT NULL,
    [resource_type] int NOT NULL,
    [category_id] uniqueidentifier NOT NULL,
    [country_id] uniqueidentifier NULL,
    [uploaded_by_id] uniqueidentifier NOT NULL,
    [asset_file_id] uniqueidentifier NOT NULL,
    [published_on] datetimeoffset NULL,
    [view_count] bigint NOT NULL,
    [row_version] rowversion NOT NULL,
    [is_deleted] bit NOT NULL,
    [deleted_on] datetimeoffset NULL,
    [deleted_by_id] uniqueidentifier NULL,
    CONSTRAINT [pk_resources] PRIMARY KEY ([id])
);

CREATE TABLE [search_query_logs] (
    [id] uniqueidentifier NOT NULL,
    [user_id] uniqueidentifier NULL,
    [query_text] nvarchar(1000) NOT NULL,
    [results_count] int NOT NULL,
    [response_time_ms] int NOT NULL,
    [locale] nvarchar(2) NOT NULL,
    [submitted_on] datetimeoffset NOT NULL,
    CONSTRAINT [pk_search_query_logs] PRIMARY KEY ([id])
);

CREATE TABLE [service_ratings] (
    [id] uniqueidentifier NOT NULL,
    [user_id] uniqueidentifier NULL,
    [rating] int NOT NULL,
    [comment_ar] nvarchar(2000) NULL,
    [comment_en] nvarchar(2000) NULL,
    [page] nvarchar(256) NOT NULL,
    [locale] nvarchar(2) NOT NULL,
    [submitted_on] datetimeoffset NOT NULL,
    CONSTRAINT [pk_service_ratings] PRIMARY KEY ([id])
);

CREATE TABLE [state_representative_assignments] (
    [id] uniqueidentifier NOT NULL,
    [user_id] uniqueidentifier NOT NULL,
    [country_id] uniqueidentifier NOT NULL,
    [assigned_on] datetimeoffset NOT NULL,
    [assigned_by_id] uniqueidentifier NOT NULL,
    [revoked_on] datetimeoffset NULL,
    [revoked_by_id] uniqueidentifier NULL,
    [is_deleted] bit NOT NULL,
    [deleted_on] datetimeoffset NULL,
    [deleted_by_id] uniqueidentifier NULL,
    CONSTRAINT [pk_state_representative_assignments] PRIMARY KEY ([id])
);

CREATE TABLE [topic_follows] (
    [id] uniqueidentifier NOT NULL,
    [topic_id] uniqueidentifier NOT NULL,
    [user_id] uniqueidentifier NOT NULL,
    [followed_on] datetimeoffset NOT NULL,
    CONSTRAINT [pk_topic_follows] PRIMARY KEY ([id])
);

CREATE TABLE [topics] (
    [id] uniqueidentifier NOT NULL,
    [name_ar] nvarchar(256) NOT NULL,
    [name_en] nvarchar(256) NOT NULL,
    [description_ar] nvarchar(max) NOT NULL,
    [description_en] nvarchar(max) NOT NULL,
    [slug] nvarchar(128) NOT NULL,
    [parent_id] uniqueidentifier NULL,
    [icon_url] nvarchar(2048) NULL,
    [order_index] int NOT NULL,
    [is_active] bit NOT NULL,
    [is_deleted] bit NOT NULL,
    [deleted_on] datetimeoffset NULL,
    [deleted_by_id] uniqueidentifier NULL,
    CONSTRAINT [pk_topics] PRIMARY KEY ([id])
);

CREATE TABLE [user_follows] (
    [id] uniqueidentifier NOT NULL,
    [follower_id] uniqueidentifier NOT NULL,
    [followed_id] uniqueidentifier NOT NULL,
    [followed_on] datetimeoffset NOT NULL,
    CONSTRAINT [pk_user_follows] PRIMARY KEY ([id])
);

CREATE TABLE [user_notifications] (
    [id] uniqueidentifier NOT NULL,
    [user_id] uniqueidentifier NOT NULL,
    [template_id] uniqueidentifier NOT NULL,
    [rendered_subject_ar] nvarchar(512) NOT NULL,
    [rendered_subject_en] nvarchar(512) NOT NULL,
    [rendered_body] nvarchar(max) NOT NULL,
    [rendered_locale] nvarchar(2) NOT NULL,
    [channel] int NOT NULL,
    [sent_on] datetimeoffset NULL,
    [read_on] datetimeoffset NULL,
    [status] int NOT NULL,
    CONSTRAINT [pk_user_notifications] PRIMARY KEY ([id])
);

CREATE TABLE [AspNetRoleClaims] (
    [id] int NOT NULL IDENTITY,
    [role_id] uniqueidentifier NOT NULL,
    [claim_type] nvarchar(max) NULL,
    [claim_value] nvarchar(max) NULL,
    CONSTRAINT [pk_asp_net_role_claims] PRIMARY KEY ([id]),
    CONSTRAINT [fk_asp_net_role_claims_asp_net_roles_role_id] FOREIGN KEY ([role_id]) REFERENCES [AspNetRoles] ([id]) ON DELETE CASCADE
);

CREATE TABLE [AspNetUserClaims] (
    [id] int NOT NULL IDENTITY,
    [user_id] uniqueidentifier NOT NULL,
    [claim_type] nvarchar(max) NULL,
    [claim_value] nvarchar(max) NULL,
    CONSTRAINT [pk_asp_net_user_claims] PRIMARY KEY ([id]),
    CONSTRAINT [fk_asp_net_user_claims_asp_net_users_user_id] FOREIGN KEY ([user_id]) REFERENCES [AspNetUsers] ([id]) ON DELETE CASCADE
);

CREATE TABLE [AspNetUserLogins] (
    [login_provider] nvarchar(450) NOT NULL,
    [provider_key] nvarchar(450) NOT NULL,
    [provider_display_name] nvarchar(max) NULL,
    [user_id] uniqueidentifier NOT NULL,
    CONSTRAINT [pk_asp_net_user_logins] PRIMARY KEY ([login_provider], [provider_key]),
    CONSTRAINT [fk_asp_net_user_logins_asp_net_users_user_id] FOREIGN KEY ([user_id]) REFERENCES [AspNetUsers] ([id]) ON DELETE CASCADE
);

CREATE TABLE [AspNetUserRoles] (
    [user_id] uniqueidentifier NOT NULL,
    [role_id] uniqueidentifier NOT NULL,
    CONSTRAINT [pk_asp_net_user_roles] PRIMARY KEY ([user_id], [role_id]),
    CONSTRAINT [fk_asp_net_user_roles_asp_net_roles_role_id] FOREIGN KEY ([role_id]) REFERENCES [AspNetRoles] ([id]) ON DELETE CASCADE,
    CONSTRAINT [fk_asp_net_user_roles_asp_net_users_user_id] FOREIGN KEY ([user_id]) REFERENCES [AspNetUsers] ([id]) ON DELETE CASCADE
);

CREATE TABLE [AspNetUserTokens] (
    [user_id] uniqueidentifier NOT NULL,
    [login_provider] nvarchar(450) NOT NULL,
    [name] nvarchar(450) NOT NULL,
    [value] nvarchar(max) NULL,
    CONSTRAINT [pk_asp_net_user_tokens] PRIMARY KEY ([user_id], [login_provider], [name]),
    CONSTRAINT [fk_asp_net_user_tokens_asp_net_users_user_id] FOREIGN KEY ([user_id]) REFERENCES [AspNetUsers] ([id]) ON DELETE CASCADE
);

CREATE INDEX [ix_asp_net_role_claims_role_id] ON [AspNetRoleClaims] ([role_id]);

CREATE UNIQUE INDEX [RoleNameIndex] ON [AspNetRoles] ([normalized_name]) WHERE [normalized_name] IS NOT NULL;

CREATE INDEX [ix_asp_net_user_claims_user_id] ON [AspNetUserClaims] ([user_id]);

CREATE INDEX [ix_asp_net_user_logins_user_id] ON [AspNetUserLogins] ([user_id]);

CREATE INDEX [ix_asp_net_user_roles_role_id] ON [AspNetUserRoles] ([role_id]);

CREATE INDEX [EmailIndex] ON [AspNetUsers] ([normalized_email]);

CREATE INDEX [ix_users_country_id] ON [AspNetUsers] ([country_id]);

CREATE UNIQUE INDEX [UserNameIndex] ON [AspNetUsers] ([normalized_user_name]) WHERE [normalized_user_name] IS NOT NULL;

CREATE INDEX [ix_asset_file_scan_status] ON [asset_files] ([virus_scan_status]);

CREATE INDEX [ix_city_result_scenario_at] ON [city_scenario_results] ([scenario_id], [computed_at]);

CREATE INDEX [ix_city_scenario_user_modified] ON [city_scenarios] ([user_id], [last_modified_on]);

CREATE INDEX [ix_city_tech_is_active] ON [city_technologies] ([is_active]);

CREATE INDEX [ix_country_iso_alpha2] ON [countries] ([iso_alpha2]);

CREATE UNIQUE INDEX [ux_country_iso_alpha3_active] ON [countries] ([iso_alpha3]) WHERE [is_deleted] = 0;

CREATE INDEX [ix_kapsarc_snapshot_country_taken] ON [country_kapsarc_snapshots] ([country_id], [snapshot_taken_on]);

CREATE UNIQUE INDEX [ux_country_profile_country_id] ON [country_profiles] ([country_id]);

CREATE INDEX [ix_country_request_country_status] ON [country_resource_requests] ([country_id], [status]);

CREATE INDEX [ix_event_starts_on] ON [events] ([starts_on]);

CREATE UNIQUE INDEX [ux_event_ical_uid] ON [events] ([i_cal_uid]);

CREATE UNIQUE INDEX [ux_expert_profile_active_user] ON [expert_profiles] ([user_id]) WHERE [is_deleted] = 0;

CREATE INDEX [ix_expert_request_requested_by] ON [expert_registration_requests] ([requested_by_id]);

CREATE INDEX [ix_expert_request_status] ON [expert_registration_requests] ([status]);

CREATE INDEX [ix_homepage_section_active_order] ON [homepage_sections] ([is_active], [order_index]);

CREATE UNIQUE INDEX [ux_km_assoc_node_type_id] ON [knowledge_map_associations] ([node_id], [associated_type], [associated_id]);

CREATE INDEX [ix_km_edge_from_node] ON [knowledge_map_edges] ([from_node_id]);

CREATE INDEX [ix_km_edge_to_node] ON [knowledge_map_edges] ([to_node_id]);

CREATE UNIQUE INDEX [ux_km_edge_map_from_to_relation] ON [knowledge_map_edges] ([map_id], [from_node_id], [to_node_id], [relationship_type]);

CREATE INDEX [ix_km_node_map_order] ON [knowledge_map_nodes] ([map_id], [order_index]);

CREATE UNIQUE INDEX [ux_knowledge_map_slug_active] ON [knowledge_maps] ([slug]) WHERE [is_deleted] = 0;

CREATE INDEX [ix_news_published_on] ON [news] ([published_on]);

CREATE UNIQUE INDEX [ux_news_slug_active] ON [news] ([slug]) WHERE [is_deleted] = 0;

CREATE INDEX [ix_newsletter_token] ON [newsletter_subscriptions] ([confirmation_token]);

CREATE UNIQUE INDEX [ux_newsletter_email] ON [newsletter_subscriptions] ([email]);

CREATE UNIQUE INDEX [ux_notification_template_code] ON [notification_templates] ([code]);

CREATE UNIQUE INDEX [ux_page_type_slug_active] ON [pages] ([page_type], [slug]) WHERE [is_deleted] = 0;

CREATE UNIQUE INDEX [ux_post_follow_post_user] ON [post_follows] ([post_id], [user_id]);

CREATE UNIQUE INDEX [ux_post_rating_post_user] ON [post_ratings] ([post_id], [user_id]);

CREATE INDEX [ix_post_reply_parent_id] ON [post_replies] ([parent_reply_id]);

CREATE INDEX [ix_post_reply_post_id] ON [post_replies] ([post_id]);

CREATE INDEX [ix_post_author_created] ON [posts] ([author_id], [created_on]);

CREATE INDEX [ix_post_topic_id] ON [posts] ([topic_id]);

CREATE INDEX [ix_resource_category_parent_id] ON [resource_categories] ([parent_id]);

CREATE UNIQUE INDEX [ux_resource_category_slug] ON [resource_categories] ([slug]);

CREATE INDEX [ix_resource_asset_file_id] ON [resources] ([asset_file_id]);

CREATE INDEX [ix_resource_category_published] ON [resources] ([category_id], [published_on]);

CREATE INDEX [ix_resource_country_id] ON [resources] ([country_id]);

CREATE INDEX [ix_search_query_log_submitted_on] ON [search_query_logs] ([submitted_on]);

CREATE INDEX [ix_service_rating_submitted_on] ON [service_ratings] ([submitted_on]);

CREATE INDEX [ix_state_rep_country_id] ON [state_representative_assignments] ([country_id]);

CREATE INDEX [ix_state_rep_user_id] ON [state_representative_assignments] ([user_id]);

CREATE UNIQUE INDEX [ux_state_rep_active_user_country] ON [state_representative_assignments] ([user_id], [country_id]) WHERE [is_deleted] = 0;

CREATE UNIQUE INDEX [ux_topic_follow_topic_user] ON [topic_follows] ([topic_id], [user_id]);

CREATE UNIQUE INDEX [ux_topic_slug_active] ON [topics] ([slug]) WHERE [is_deleted] = 0;

CREATE UNIQUE INDEX [ux_user_follow_follower_followed] ON [user_follows] ([follower_id], [followed_id]);

CREATE INDEX [ix_user_notification_user_status] ON [user_notifications] ([user_id], [status]);

INSERT INTO [__EFMigrationsHistory] ([migration_id], [product_version])
VALUES (N'20260427192344_DataDomainInitial', N'10.0.1');

COMMIT;
GO

BEGIN TRANSACTION;
ALTER TABLE [AspNetUsers] ADD [entra_id_object_id] uniqueidentifier NULL;

CREATE UNIQUE INDEX [ix_asp_net_users_entra_id_object_id] ON [AspNetUsers] ([entra_id_object_id]) WHERE [entra_id_object_id] IS NOT NULL;

INSERT INTO [__EFMigrationsHistory] ([migration_id], [product_version])
VALUES (N'20260504182534_AddEntraIdObjectIdToUser', N'10.0.1');

COMMIT;
GO

BEGIN TRANSACTION;
ALTER TABLE [AspNetUsers] ADD [first_name] nvarchar(50) NOT NULL DEFAULT N'';

ALTER TABLE [AspNetUsers] ADD [job_title] nvarchar(50) NOT NULL DEFAULT N'';

ALTER TABLE [AspNetUsers] ADD [last_name] nvarchar(50) NOT NULL DEFAULT N'';

ALTER TABLE [AspNetUsers] ADD [organization_name] nvarchar(100) NOT NULL DEFAULT N'';

CREATE TABLE [refresh_tokens] (
    [id] uniqueidentifier NOT NULL,
    [user_id] uniqueidentifier NOT NULL,
    [token_hash] nvarchar(128) NOT NULL,
    [token_family_id] uniqueidentifier NOT NULL,
    [created_at_utc] datetimeoffset NOT NULL,
    [expires_at_utc] datetimeoffset NOT NULL,
    [revoked_at_utc] datetimeoffset NULL,
    [replaced_by_token_hash] nvarchar(128) NULL,
    [created_by_ip] nvarchar(64) NULL,
    [revoked_by_ip] nvarchar(64) NULL,
    [user_agent] nvarchar(512) NULL,
    CONSTRAINT [pk_refresh_tokens] PRIMARY KEY ([id]),
    CONSTRAINT [fk_refresh_tokens_asp_net_users_user_id] FOREIGN KEY ([user_id]) REFERENCES [AspNetUsers] ([id]) ON DELETE CASCADE
);

CREATE INDEX [ix_refresh_tokens_token_family_id] ON [refresh_tokens] ([token_family_id]);

CREATE INDEX [ix_refresh_tokens_user_id] ON [refresh_tokens] ([user_id]);

CREATE UNIQUE INDEX [ux_refresh_tokens_token_hash] ON [refresh_tokens] ([token_hash]);

INSERT INTO [__EFMigrationsHistory] ([migration_id], [product_version])
VALUES (N'20260514202038_AddLocalAuthRefreshTokens', N'10.0.1');

COMMIT;
GO

BEGIN TRANSACTION;

                IF EXISTS (
                    SELECT 1 FROM sys.columns c
                    JOIN sys.tables t ON c.object_id = t.object_id
                    WHERE t.name = 'country_profiles' AND c.name = 'last_updated_on'
                )
                BEGIN
                    EXEC sp_rename N'[country_profiles].[last_updated_on]', N'created_on', 'COLUMN';
                END

                IF EXISTS (
                    SELECT 1 FROM sys.columns c
                    JOIN sys.tables t ON c.object_id = t.object_id
                    WHERE t.name = 'country_profiles' AND c.name = 'last_updated_by_id'
                )
                BEGIN
                    EXEC sp_rename N'[country_profiles].[last_updated_by_id]', N'created_by_id', 'COLUMN';
                END
            

ALTER TABLE [topics] ADD [created_by_id] uniqueidentifier NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';

ALTER TABLE [topics] ADD [created_on] datetimeoffset NOT NULL DEFAULT '0001-01-01T00:00:00.0000000+00:00';

ALTER TABLE [topics] ADD [last_modified_by_id] uniqueidentifier NULL;

ALTER TABLE [topics] ADD [last_modified_on] datetimeoffset NULL;

ALTER TABLE [state_representative_assignments] ADD [created_by_id] uniqueidentifier NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';

ALTER TABLE [state_representative_assignments] ADD [created_on] datetimeoffset NOT NULL DEFAULT '0001-01-01T00:00:00.0000000+00:00';

ALTER TABLE [state_representative_assignments] ADD [last_modified_by_id] uniqueidentifier NULL;

ALTER TABLE [state_representative_assignments] ADD [last_modified_on] datetimeoffset NULL;

ALTER TABLE [resources] ADD [created_by_id] uniqueidentifier NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';

ALTER TABLE [resources] ADD [created_on] datetimeoffset NOT NULL DEFAULT '0001-01-01T00:00:00.0000000+00:00';

ALTER TABLE [resources] ADD [last_modified_by_id] uniqueidentifier NULL;

ALTER TABLE [resources] ADD [last_modified_on] datetimeoffset NULL;

ALTER TABLE [posts] ADD [created_by_id] uniqueidentifier NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';

ALTER TABLE [posts] ADD [last_modified_by_id] uniqueidentifier NULL;

ALTER TABLE [posts] ADD [last_modified_on] datetimeoffset NULL;

ALTER TABLE [post_replies] ADD [created_by_id] uniqueidentifier NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';

ALTER TABLE [post_replies] ADD [last_modified_by_id] uniqueidentifier NULL;

ALTER TABLE [post_replies] ADD [last_modified_on] datetimeoffset NULL;

ALTER TABLE [pages] ADD [created_by_id] uniqueidentifier NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';

ALTER TABLE [pages] ADD [created_on] datetimeoffset NOT NULL DEFAULT '0001-01-01T00:00:00.0000000+00:00';

ALTER TABLE [pages] ADD [last_modified_by_id] uniqueidentifier NULL;

ALTER TABLE [pages] ADD [last_modified_on] datetimeoffset NULL;

ALTER TABLE [news] ADD [created_by_id] uniqueidentifier NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';

ALTER TABLE [news] ADD [created_on] datetimeoffset NOT NULL DEFAULT '0001-01-01T00:00:00.0000000+00:00';

ALTER TABLE [news] ADD [last_modified_by_id] uniqueidentifier NULL;

ALTER TABLE [news] ADD [last_modified_on] datetimeoffset NULL;

ALTER TABLE [knowledge_maps] ADD [created_by_id] uniqueidentifier NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';

ALTER TABLE [knowledge_maps] ADD [created_on] datetimeoffset NOT NULL DEFAULT '0001-01-01T00:00:00.0000000+00:00';

ALTER TABLE [knowledge_maps] ADD [last_modified_by_id] uniqueidentifier NULL;

ALTER TABLE [knowledge_maps] ADD [last_modified_on] datetimeoffset NULL;

ALTER TABLE [homepage_sections] ADD [created_by_id] uniqueidentifier NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';

ALTER TABLE [homepage_sections] ADD [created_on] datetimeoffset NOT NULL DEFAULT '0001-01-01T00:00:00.0000000+00:00';

ALTER TABLE [homepage_sections] ADD [last_modified_by_id] uniqueidentifier NULL;

ALTER TABLE [homepage_sections] ADD [last_modified_on] datetimeoffset NULL;

ALTER TABLE [expert_registration_requests] ADD [created_by_id] uniqueidentifier NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';

ALTER TABLE [expert_registration_requests] ADD [created_on] datetimeoffset NOT NULL DEFAULT '0001-01-01T00:00:00.0000000+00:00';

ALTER TABLE [expert_registration_requests] ADD [last_modified_by_id] uniqueidentifier NULL;

ALTER TABLE [expert_registration_requests] ADD [last_modified_on] datetimeoffset NULL;

ALTER TABLE [expert_profiles] ADD [created_by_id] uniqueidentifier NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';

ALTER TABLE [expert_profiles] ADD [created_on] datetimeoffset NOT NULL DEFAULT '0001-01-01T00:00:00.0000000+00:00';

ALTER TABLE [expert_profiles] ADD [last_modified_by_id] uniqueidentifier NULL;

ALTER TABLE [expert_profiles] ADD [last_modified_on] datetimeoffset NULL;

ALTER TABLE [events] ADD [created_by_id] uniqueidentifier NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';

ALTER TABLE [events] ADD [created_on] datetimeoffset NOT NULL DEFAULT '0001-01-01T00:00:00.0000000+00:00';

ALTER TABLE [events] ADD [last_modified_by_id] uniqueidentifier NULL;

ALTER TABLE [events] ADD [last_modified_on] datetimeoffset NULL;

ALTER TABLE [country_resource_requests] ADD [created_by_id] uniqueidentifier NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';

ALTER TABLE [country_resource_requests] ADD [created_on] datetimeoffset NOT NULL DEFAULT '0001-01-01T00:00:00.0000000+00:00';

ALTER TABLE [country_resource_requests] ADD [last_modified_by_id] uniqueidentifier NULL;

ALTER TABLE [country_resource_requests] ADD [last_modified_on] datetimeoffset NULL;

ALTER TABLE [country_profiles] ADD [last_modified_by_id] uniqueidentifier NULL;

ALTER TABLE [country_profiles] ADD [last_modified_on] datetimeoffset NULL;

ALTER TABLE [countries] ADD [created_by_id] uniqueidentifier NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';

ALTER TABLE [countries] ADD [created_on] datetimeoffset NOT NULL DEFAULT '0001-01-01T00:00:00.0000000+00:00';

ALTER TABLE [countries] ADD [last_modified_by_id] uniqueidentifier NULL;

ALTER TABLE [countries] ADD [last_modified_on] datetimeoffset NULL;

DECLARE @var nvarchar(max);
SELECT @var = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[city_scenarios]') AND [c].[name] = N'last_modified_on');
IF @var IS NOT NULL EXEC(N'ALTER TABLE [city_scenarios] DROP CONSTRAINT ' + @var + ';');
ALTER TABLE [city_scenarios] ALTER COLUMN [last_modified_on] datetimeoffset NULL;

ALTER TABLE [city_scenarios] ADD [created_by_id] uniqueidentifier NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';

ALTER TABLE [city_scenarios] ADD [last_modified_by_id] uniqueidentifier NULL;

INSERT INTO [__EFMigrationsHistory] ([migration_id], [product_version])
VALUES (N'20260515121258_StandardizeCountryProfileAudit', N'10.0.1');

COMMIT;
GO

BEGIN TRANSACTION;
EXEC sp_rename N'[country_profiles].[last_updated_on]', N'created_on', 'COLUMN';

EXEC sp_rename N'[country_profiles].[last_updated_by_id]', N'created_by_id', 'COLUMN';

ALTER TABLE [topics] ADD [created_by_id] uniqueidentifier NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';

ALTER TABLE [topics] ADD [created_on] datetimeoffset NOT NULL DEFAULT '0001-01-01T00:00:00.0000000+00:00';

ALTER TABLE [topics] ADD [last_modified_by_id] uniqueidentifier NULL;

ALTER TABLE [topics] ADD [last_modified_on] datetimeoffset NULL;

ALTER TABLE [state_representative_assignments] ADD [created_by_id] uniqueidentifier NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';

ALTER TABLE [state_representative_assignments] ADD [created_on] datetimeoffset NOT NULL DEFAULT '0001-01-01T00:00:00.0000000+00:00';

ALTER TABLE [state_representative_assignments] ADD [last_modified_by_id] uniqueidentifier NULL;

ALTER TABLE [state_representative_assignments] ADD [last_modified_on] datetimeoffset NULL;

ALTER TABLE [resources] ADD [created_by_id] uniqueidentifier NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';

ALTER TABLE [resources] ADD [created_on] datetimeoffset NOT NULL DEFAULT '0001-01-01T00:00:00.0000000+00:00';

ALTER TABLE [resources] ADD [last_modified_by_id] uniqueidentifier NULL;

ALTER TABLE [resources] ADD [last_modified_on] datetimeoffset NULL;

ALTER TABLE [posts] ADD [created_by_id] uniqueidentifier NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';

ALTER TABLE [posts] ADD [last_modified_by_id] uniqueidentifier NULL;

ALTER TABLE [posts] ADD [last_modified_on] datetimeoffset NULL;

ALTER TABLE [post_replies] ADD [created_by_id] uniqueidentifier NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';

ALTER TABLE [post_replies] ADD [last_modified_by_id] uniqueidentifier NULL;

ALTER TABLE [post_replies] ADD [last_modified_on] datetimeoffset NULL;

ALTER TABLE [pages] ADD [created_by_id] uniqueidentifier NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';

ALTER TABLE [pages] ADD [created_on] datetimeoffset NOT NULL DEFAULT '0001-01-01T00:00:00.0000000+00:00';

ALTER TABLE [pages] ADD [last_modified_by_id] uniqueidentifier NULL;

ALTER TABLE [pages] ADD [last_modified_on] datetimeoffset NULL;

ALTER TABLE [newsletter_subscriptions] ADD [created_by_id] uniqueidentifier NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';

ALTER TABLE [newsletter_subscriptions] ADD [created_on] datetimeoffset NOT NULL DEFAULT '0001-01-01T00:00:00.0000000+00:00';

ALTER TABLE [newsletter_subscriptions] ADD [deleted_by_id] uniqueidentifier NULL;

ALTER TABLE [newsletter_subscriptions] ADD [deleted_on] datetimeoffset NULL;

ALTER TABLE [newsletter_subscriptions] ADD [is_deleted] bit NOT NULL DEFAULT CAST(0 AS bit);

ALTER TABLE [newsletter_subscriptions] ADD [last_modified_by_id] uniqueidentifier NULL;

ALTER TABLE [newsletter_subscriptions] ADD [last_modified_on] datetimeoffset NULL;

ALTER TABLE [news] ADD [created_by_id] uniqueidentifier NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';

ALTER TABLE [news] ADD [created_on] datetimeoffset NOT NULL DEFAULT '0001-01-01T00:00:00.0000000+00:00';

ALTER TABLE [news] ADD [last_modified_by_id] uniqueidentifier NULL;

ALTER TABLE [news] ADD [last_modified_on] datetimeoffset NULL;

ALTER TABLE [knowledge_maps] ADD [created_by_id] uniqueidentifier NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';

ALTER TABLE [knowledge_maps] ADD [created_on] datetimeoffset NOT NULL DEFAULT '0001-01-01T00:00:00.0000000+00:00';

ALTER TABLE [knowledge_maps] ADD [last_modified_by_id] uniqueidentifier NULL;

ALTER TABLE [knowledge_maps] ADD [last_modified_on] datetimeoffset NULL;

ALTER TABLE [homepage_sections] ADD [created_by_id] uniqueidentifier NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';

ALTER TABLE [homepage_sections] ADD [created_on] datetimeoffset NOT NULL DEFAULT '0001-01-01T00:00:00.0000000+00:00';

ALTER TABLE [homepage_sections] ADD [last_modified_by_id] uniqueidentifier NULL;

ALTER TABLE [homepage_sections] ADD [last_modified_on] datetimeoffset NULL;

ALTER TABLE [expert_registration_requests] ADD [created_by_id] uniqueidentifier NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';

ALTER TABLE [expert_registration_requests] ADD [created_on] datetimeoffset NOT NULL DEFAULT '0001-01-01T00:00:00.0000000+00:00';

ALTER TABLE [expert_registration_requests] ADD [last_modified_by_id] uniqueidentifier NULL;

ALTER TABLE [expert_registration_requests] ADD [last_modified_on] datetimeoffset NULL;

ALTER TABLE [expert_profiles] ADD [created_by_id] uniqueidentifier NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';

ALTER TABLE [expert_profiles] ADD [created_on] datetimeoffset NOT NULL DEFAULT '0001-01-01T00:00:00.0000000+00:00';

ALTER TABLE [expert_profiles] ADD [last_modified_by_id] uniqueidentifier NULL;

ALTER TABLE [expert_profiles] ADD [last_modified_on] datetimeoffset NULL;

ALTER TABLE [events] ADD [created_by_id] uniqueidentifier NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';

ALTER TABLE [events] ADD [created_on] datetimeoffset NOT NULL DEFAULT '0001-01-01T00:00:00.0000000+00:00';

ALTER TABLE [events] ADD [last_modified_by_id] uniqueidentifier NULL;

ALTER TABLE [events] ADD [last_modified_on] datetimeoffset NULL;

ALTER TABLE [country_resource_requests] ADD [created_by_id] uniqueidentifier NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';

ALTER TABLE [country_resource_requests] ADD [created_on] datetimeoffset NOT NULL DEFAULT '0001-01-01T00:00:00.0000000+00:00';

ALTER TABLE [country_resource_requests] ADD [last_modified_by_id] uniqueidentifier NULL;

ALTER TABLE [country_resource_requests] ADD [last_modified_on] datetimeoffset NULL;

ALTER TABLE [country_profiles] ADD [last_modified_by_id] uniqueidentifier NULL;

ALTER TABLE [country_profiles] ADD [last_modified_on] datetimeoffset NULL;

ALTER TABLE [countries] ADD [created_by_id] uniqueidentifier NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';

ALTER TABLE [countries] ADD [created_on] datetimeoffset NOT NULL DEFAULT '0001-01-01T00:00:00.0000000+00:00';

ALTER TABLE [countries] ADD [last_modified_by_id] uniqueidentifier NULL;

ALTER TABLE [countries] ADD [last_modified_on] datetimeoffset NULL;

DECLARE @var1 nvarchar(max);
SELECT @var1 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[city_scenarios]') AND [c].[name] = N'last_modified_on');
IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [city_scenarios] DROP CONSTRAINT ' + @var1 + ';');
ALTER TABLE [city_scenarios] ALTER COLUMN [last_modified_on] datetimeoffset NULL;

ALTER TABLE [city_scenarios] ADD [created_by_id] uniqueidentifier NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';

ALTER TABLE [city_scenarios] ADD [last_modified_by_id] uniqueidentifier NULL;

ALTER TABLE [AspNetUsers] ADD [status] int NOT NULL DEFAULT 0;

INSERT INTO [__EFMigrationsHistory] ([migration_id], [product_version])
VALUES (N'20260520101638_AddUserStatus', N'10.0.1');

COMMIT;
GO

BEGIN TRANSACTION;
ALTER TABLE [AspNetUsers] ADD [deleted_by_id] uniqueidentifier NULL;

ALTER TABLE [AspNetUsers] ADD [deleted_on] datetimeoffset NULL;

ALTER TABLE [AspNetUsers] ADD [is_deleted] bit NOT NULL DEFAULT CAST(0 AS bit);

INSERT INTO [__EFMigrationsHistory] ([migration_id], [product_version])
VALUES (N'20260520111756_AddUserSoftDelete', N'10.0.1');

COMMIT;
GO

BEGIN TRANSACTION;
CREATE TABLE [about_settings] (
    [id] uniqueidentifier NOT NULL,
    [description_ar] nvarchar(1000) NOT NULL,
    [description_en] nvarchar(1000) NOT NULL,
    [how_to_use_video_url] nvarchar(max) NULL,
    [row_version] rowversion NOT NULL,
    [created_on] datetimeoffset NOT NULL,
    [created_by_id] uniqueidentifier NOT NULL,
    [last_modified_on] datetimeoffset NULL,
    [last_modified_by_id] uniqueidentifier NULL,
    [is_deleted] bit NOT NULL,
    [deleted_on] datetimeoffset NULL,
    [deleted_by_id] uniqueidentifier NULL,
    CONSTRAINT [pk_about_settings] PRIMARY KEY ([id])
);

CREATE TABLE [glossary_entries] (
    [id] uniqueidentifier NOT NULL,
    [about_settings_id] uniqueidentifier NOT NULL,
    [term_ar] nvarchar(100) NOT NULL,
    [term_en] nvarchar(100) NOT NULL,
    [definition_ar] nvarchar(1000) NOT NULL,
    [definition_en] nvarchar(1000) NOT NULL,
    [order_index] int NOT NULL,
    [created_on] datetimeoffset NOT NULL,
    [created_by_id] uniqueidentifier NOT NULL,
    [last_modified_on] datetimeoffset NULL,
    [last_modified_by_id] uniqueidentifier NULL,
    [is_deleted] bit NOT NULL,
    [deleted_on] datetimeoffset NULL,
    [deleted_by_id] uniqueidentifier NULL,
    CONSTRAINT [pk_glossary_entries] PRIMARY KEY ([id])
);

CREATE TABLE [homepage_countries] (
    [id] uniqueidentifier NOT NULL,
    [homepage_settings_id] uniqueidentifier NOT NULL,
    [country_id] uniqueidentifier NOT NULL,
    [order_index] int NOT NULL,
    CONSTRAINT [pk_homepage_countries] PRIMARY KEY ([id])
);

CREATE TABLE [homepage_settings] (
    [id] uniqueidentifier NOT NULL,
    [video_url] nvarchar(max) NULL,
    [objective_ar] nvarchar(1000) NOT NULL,
    [objective_en] nvarchar(1000) NOT NULL,
    [cce_concepts_ar] nvarchar(max) NOT NULL,
    [cce_concepts_en] nvarchar(max) NOT NULL,
    [row_version] rowversion NOT NULL,
    [created_on] datetimeoffset NOT NULL,
    [created_by_id] uniqueidentifier NOT NULL,
    [last_modified_on] datetimeoffset NULL,
    [last_modified_by_id] uniqueidentifier NULL,
    [is_deleted] bit NOT NULL,
    [deleted_on] datetimeoffset NULL,
    [deleted_by_id] uniqueidentifier NULL,
    CONSTRAINT [pk_homepage_settings] PRIMARY KEY ([id])
);

CREATE TABLE [knowledge_partners] (
    [id] uniqueidentifier NOT NULL,
    [about_settings_id] uniqueidentifier NOT NULL,
    [name_ar] nvarchar(200) NOT NULL,
    [name_en] nvarchar(200) NOT NULL,
    [logo_url] nvarchar(max) NULL,
    [website_url] nvarchar(max) NULL,
    [description_ar] nvarchar(1000) NULL,
    [description_en] nvarchar(1000) NULL,
    [order_index] int NOT NULL,
    [created_on] datetimeoffset NOT NULL,
    [created_by_id] uniqueidentifier NOT NULL,
    [last_modified_on] datetimeoffset NULL,
    [last_modified_by_id] uniqueidentifier NULL,
    [is_deleted] bit NOT NULL,
    [deleted_on] datetimeoffset NULL,
    [deleted_by_id] uniqueidentifier NULL,
    CONSTRAINT [pk_knowledge_partners] PRIMARY KEY ([id])
);

CREATE TABLE [policies_settings] (
    [id] uniqueidentifier NOT NULL,
    [row_version] rowversion NOT NULL,
    [created_on] datetimeoffset NOT NULL,
    [created_by_id] uniqueidentifier NOT NULL,
    [last_modified_on] datetimeoffset NULL,
    [last_modified_by_id] uniqueidentifier NULL,
    [is_deleted] bit NOT NULL,
    [deleted_on] datetimeoffset NULL,
    [deleted_by_id] uniqueidentifier NULL,
    CONSTRAINT [pk_policies_settings] PRIMARY KEY ([id])
);

CREATE TABLE [policy_sections] (
    [id] uniqueidentifier NOT NULL,
    [policies_settings_id] uniqueidentifier NOT NULL,
    [type] int NOT NULL,
    [title_ar] nvarchar(500) NOT NULL,
    [title_en] nvarchar(500) NOT NULL,
    [content_ar] nvarchar(max) NOT NULL,
    [content_en] nvarchar(max) NOT NULL,
    [order_index] int NOT NULL,
    [created_on] datetimeoffset NOT NULL,
    [created_by_id] uniqueidentifier NOT NULL,
    [last_modified_on] datetimeoffset NULL,
    [last_modified_by_id] uniqueidentifier NULL,
    [is_deleted] bit NOT NULL,
    [deleted_on] datetimeoffset NULL,
    [deleted_by_id] uniqueidentifier NULL,
    CONSTRAINT [pk_policy_sections] PRIMARY KEY ([id])
);

CREATE UNIQUE INDEX [ix_homepage_country_settings_country] ON [homepage_countries] ([homepage_settings_id], [country_id]);

INSERT INTO [__EFMigrationsHistory] ([migration_id], [product_version])
VALUES (N'20260521094531_AddPlatformSettings', N'10.0.1');

COMMIT;
GO

BEGIN TRANSACTION;
CREATE TABLE [media_files] (
    [id] uniqueidentifier NOT NULL,
    [storage_key] nvarchar(500) NOT NULL,
    [url] nvarchar(2048) NOT NULL,
    [original_file_name] nvarchar(255) NOT NULL,
    [mime_type] nvarchar(100) NOT NULL,
    [size_bytes] bigint NOT NULL,
    [title_ar] nvarchar(200) NULL,
    [title_en] nvarchar(200) NULL,
    [description_ar] nvarchar(1000) NULL,
    [description_en] nvarchar(1000) NULL,
    [alt_text_ar] nvarchar(500) NULL,
    [alt_text_en] nvarchar(500) NULL,
    [uploaded_by_id] uniqueidentifier NOT NULL,
    [uploaded_on] datetimeoffset NOT NULL,
    CONSTRAINT [pk_media_files] PRIMARY KEY ([id])
);

INSERT INTO [__EFMigrationsHistory] ([migration_id], [product_version])
VALUES (N'20260521111720_AddMediaService', N'10.0.1');

COMMIT;
GO

BEGIN TRANSACTION;
DECLARE @var2 nvarchar(max);
SELECT @var2 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[policy_sections]') AND [c].[name] = N'deleted_by_id');
IF @var2 IS NOT NULL EXEC(N'ALTER TABLE [policy_sections] DROP CONSTRAINT ' + @var2 + ';');
ALTER TABLE [policy_sections] DROP COLUMN [deleted_by_id];

DECLARE @var3 nvarchar(max);
SELECT @var3 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[policy_sections]') AND [c].[name] = N'deleted_on');
IF @var3 IS NOT NULL EXEC(N'ALTER TABLE [policy_sections] DROP CONSTRAINT ' + @var3 + ';');
ALTER TABLE [policy_sections] DROP COLUMN [deleted_on];

DECLARE @var4 nvarchar(max);
SELECT @var4 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[policy_sections]') AND [c].[name] = N'is_deleted');
IF @var4 IS NOT NULL EXEC(N'ALTER TABLE [policy_sections] DROP CONSTRAINT ' + @var4 + ';');
ALTER TABLE [policy_sections] DROP COLUMN [is_deleted];

DECLARE @var5 nvarchar(max);
SELECT @var5 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[knowledge_partners]') AND [c].[name] = N'deleted_by_id');
IF @var5 IS NOT NULL EXEC(N'ALTER TABLE [knowledge_partners] DROP CONSTRAINT ' + @var5 + ';');
ALTER TABLE [knowledge_partners] DROP COLUMN [deleted_by_id];

DECLARE @var6 nvarchar(max);
SELECT @var6 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[knowledge_partners]') AND [c].[name] = N'deleted_on');
IF @var6 IS NOT NULL EXEC(N'ALTER TABLE [knowledge_partners] DROP CONSTRAINT ' + @var6 + ';');
ALTER TABLE [knowledge_partners] DROP COLUMN [deleted_on];

DECLARE @var7 nvarchar(max);
SELECT @var7 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[knowledge_partners]') AND [c].[name] = N'is_deleted');
IF @var7 IS NOT NULL EXEC(N'ALTER TABLE [knowledge_partners] DROP CONSTRAINT ' + @var7 + ';');
ALTER TABLE [knowledge_partners] DROP COLUMN [is_deleted];

DECLARE @var8 nvarchar(max);
SELECT @var8 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[glossary_entries]') AND [c].[name] = N'deleted_by_id');
IF @var8 IS NOT NULL EXEC(N'ALTER TABLE [glossary_entries] DROP CONSTRAINT ' + @var8 + ';');
ALTER TABLE [glossary_entries] DROP COLUMN [deleted_by_id];

DECLARE @var9 nvarchar(max);
SELECT @var9 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[glossary_entries]') AND [c].[name] = N'deleted_on');
IF @var9 IS NOT NULL EXEC(N'ALTER TABLE [glossary_entries] DROP CONSTRAINT ' + @var9 + ';');
ALTER TABLE [glossary_entries] DROP COLUMN [deleted_on];

DECLARE @var10 nvarchar(max);
SELECT @var10 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[glossary_entries]') AND [c].[name] = N'is_deleted');
IF @var10 IS NOT NULL EXEC(N'ALTER TABLE [glossary_entries] DROP CONSTRAINT ' + @var10 + ';');
ALTER TABLE [glossary_entries] DROP COLUMN [is_deleted];

ALTER TABLE [homepage_countries] ADD [created_by_id] uniqueidentifier NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';

ALTER TABLE [homepage_countries] ADD [created_on] datetimeoffset NOT NULL DEFAULT '0001-01-01T00:00:00.0000000+00:00';

ALTER TABLE [homepage_countries] ADD [last_modified_by_id] uniqueidentifier NULL;

ALTER TABLE [homepage_countries] ADD [last_modified_on] datetimeoffset NULL;

CREATE INDEX [ix_policy_sections_policies_settings_id] ON [policy_sections] ([policies_settings_id]);

CREATE INDEX [ix_knowledge_partners_about_settings_id] ON [knowledge_partners] ([about_settings_id]);

CREATE INDEX [ix_glossary_entries_about_settings_id] ON [glossary_entries] ([about_settings_id]);

ALTER TABLE [glossary_entries] ADD CONSTRAINT [fk_glossary_entries_about_settings_about_settings_id] FOREIGN KEY ([about_settings_id]) REFERENCES [about_settings] ([id]) ON DELETE CASCADE;

ALTER TABLE [homepage_countries] ADD CONSTRAINT [fk_homepage_countries_homepage_settings_homepage_settings_id] FOREIGN KEY ([homepage_settings_id]) REFERENCES [homepage_settings] ([id]) ON DELETE CASCADE;

ALTER TABLE [knowledge_partners] ADD CONSTRAINT [fk_knowledge_partners_about_settings_about_settings_id] FOREIGN KEY ([about_settings_id]) REFERENCES [about_settings] ([id]) ON DELETE CASCADE;

ALTER TABLE [policy_sections] ADD CONSTRAINT [fk_policy_sections_policies_settings_policies_settings_id] FOREIGN KEY ([policies_settings_id]) REFERENCES [policies_settings] ([id]) ON DELETE CASCADE;

INSERT INTO [__EFMigrationsHistory] ([migration_id], [product_version])
VALUES (N'20260522211302_RefactorPlatformSettings', N'10.0.1');

COMMIT;
GO

BEGIN TRANSACTION;
DROP INDEX [ux_notification_template_code] ON [notification_templates];

CREATE TABLE [notification_logs] (
    [id] uniqueidentifier NOT NULL,
    [recipient_user_id] uniqueidentifier NULL,
    [template_code] nvarchar(64) NOT NULL,
    [template_id] uniqueidentifier NULL,
    [channel] int NOT NULL,
    [status] int NOT NULL,
    [provider_message_id] nvarchar(256) NULL,
    [error] nvarchar(max) NULL,
    [attempt_count] int NOT NULL,
    [created_on] datetimeoffset NOT NULL,
    [sent_on] datetimeoffset NULL,
    [failed_on] datetimeoffset NULL,
    [correlation_id] nvarchar(64) NULL,
    [payload_json] nvarchar(max) NULL,
    CONSTRAINT [pk_notification_logs] PRIMARY KEY ([id])
);

CREATE TABLE [user_notification_settings] (
    [id] uniqueidentifier NOT NULL,
    [user_id] uniqueidentifier NOT NULL,
    [channel] int NOT NULL,
    [event_code] nvarchar(64) NULL,
    [is_enabled] bit NOT NULL,
    [updated_on] datetimeoffset NOT NULL,
    CONSTRAINT [pk_user_notification_settings] PRIMARY KEY ([id])
);

CREATE UNIQUE INDEX [ux_notification_template_code_channel] ON [notification_templates] ([code], [channel]);

CREATE INDEX [ix_notification_log_correlation_id] ON [notification_logs] ([correlation_id]);

CREATE INDEX [ix_notification_log_recipient_status_created] ON [notification_logs] ([recipient_user_id], [status], [created_on]);

CREATE INDEX [ix_notification_log_template_channel] ON [notification_logs] ([template_code], [channel]);

CREATE UNIQUE INDEX [ux_user_notification_settings_user_channel_event] ON [user_notification_settings] ([user_id], [channel], [event_code]) WHERE [event_code] IS NOT NULL;

INSERT INTO [__EFMigrationsHistory] ([migration_id], [product_version])
VALUES (N'20260523111750_AddNotificationGateway', N'10.0.1');

COMMIT;
GO

BEGIN TRANSACTION;
CREATE TABLE [otp_verifications] (
    [id] uniqueidentifier NOT NULL,
    [contact] nvarchar(256) NOT NULL,
    [type_id] int NOT NULL,
    [code_hash] nvarchar(512) NOT NULL,
    [expires_at] datetimeoffset NOT NULL,
    [created_at] datetimeoffset NOT NULL,
    [last_sent_at] datetimeoffset NULL,
    [attempt_count] int NOT NULL,
    [is_verified] bit NOT NULL,
    [is_invalidated] bit NOT NULL,
    [created_on] datetimeoffset NOT NULL,
    [created_by_id] uniqueidentifier NOT NULL,
    [last_modified_on] datetimeoffset NULL,
    [last_modified_by_id] uniqueidentifier NULL,
    [is_deleted] bit NOT NULL,
    [deleted_on] datetimeoffset NULL,
    [deleted_by_id] uniqueidentifier NULL,
    CONSTRAINT [pk_otp_verifications] PRIMARY KEY ([id])
);

CREATE TABLE [user_verifications] (
    [id] uniqueidentifier NOT NULL,
    [user_id] uniqueidentifier NULL,
    [contact] nvarchar(256) NOT NULL,
    [type_id] int NOT NULL,
    [is_verified] bit NOT NULL,
    [verified_at] datetimeoffset NULL,
    [created_on] datetimeoffset NOT NULL,
    [created_by_id] uniqueidentifier NOT NULL,
    [last_modified_on] datetimeoffset NULL,
    [last_modified_by_id] uniqueidentifier NULL,
    [is_deleted] bit NOT NULL,
    [deleted_on] datetimeoffset NULL,
    [deleted_by_id] uniqueidentifier NULL,
    CONSTRAINT [pk_user_verifications] PRIMARY KEY ([id]),
    CONSTRAINT [fk_user_verifications_asp_net_users_user_id] FOREIGN KEY ([user_id]) REFERENCES [AspNetUsers] ([id])
);

CREATE INDEX [ix_otp_verifications_contact_type_id] ON [otp_verifications] ([contact], [type_id]);

CREATE UNIQUE INDEX [ix_user_verifications_contact_type_id] ON [user_verifications] ([contact], [type_id]);

CREATE INDEX [ix_user_verifications_user_id] ON [user_verifications] ([user_id]);

INSERT INTO [__EFMigrationsHistory] ([migration_id], [product_version])
VALUES (N'20260523180351_AddOtpVerification', N'10.0.1');

COMMIT;
GO

BEGIN TRANSACTION;
CREATE TABLE [service_evaluations] (
    [id] uniqueidentifier NOT NULL,
    [overall_satisfaction] int NOT NULL,
    [ease_of_use] int NOT NULL,
    [content_suitability] int NOT NULL,
    [feedback] nvarchar(500) NOT NULL,
    [user_id] uniqueidentifier NULL,
    [created_on] datetimeoffset NOT NULL,
    [created_by_id] uniqueidentifier NOT NULL,
    [last_modified_on] datetimeoffset NULL,
    [last_modified_by_id] uniqueidentifier NULL,
    CONSTRAINT [pk_service_evaluations] PRIMARY KEY ([id])
);

CREATE INDEX [ix_service_evaluation_created_on] ON [service_evaluations] ([created_on]);

INSERT INTO [__EFMigrationsHistory] ([migration_id], [product_version])
VALUES (N'20260525092623_AddServiceEvaluation', N'10.0.1');

COMMIT;
GO

