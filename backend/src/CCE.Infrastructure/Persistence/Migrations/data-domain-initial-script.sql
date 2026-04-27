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
GO

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
GO

CREATE INDEX [ix_audit_events_actor_occurred_on] ON [audit_events] ([actor], [occurred_on]);
GO

CREATE INDEX [ix_audit_events_correlation_id] ON [audit_events] ([correlation_id]);
GO

INSERT INTO [__EFMigrationsHistory] ([migration_id], [product_version])
VALUES (N'20260425134009_InitialAuditEvents', N'8.0.10');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO


CREATE TRIGGER trg_audit_events_no_update_delete
ON dbo.audit_events
INSTEAD OF UPDATE, DELETE
AS
BEGIN
    THROW 51000, 'audit_events is append-only; UPDATE and DELETE are not permitted.', 1;
END;
GO

INSERT INTO [__EFMigrationsHistory] ([migration_id], [product_version])
VALUES (N'20260425134559_AuditEventsAppendOnlyTrigger', N'8.0.10');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

CREATE TABLE [AspNetRoles] (
    [id] uniqueidentifier NOT NULL,
    [name] nvarchar(256) NULL,
    [normalized_name] nvarchar(256) NULL,
    [concurrency_stamp] nvarchar(max) NULL,
    CONSTRAINT [pk_asp_net_roles] PRIMARY KEY ([id])
);
GO

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
GO

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
GO

CREATE TABLE [city_scenario_results] (
    [id] uniqueidentifier NOT NULL,
    [scenario_id] uniqueidentifier NOT NULL,
    [computed_carbon_neutrality_year] int NULL,
    [computed_total_cost_usd] decimal(18,2) NOT NULL,
    [computed_at] datetimeoffset NOT NULL,
    [engine_version] nvarchar(64) NOT NULL,
    CONSTRAINT [pk_city_scenario_results] PRIMARY KEY ([id])
);
GO

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
GO

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
GO

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
GO

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
GO

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
GO

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
GO

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
GO

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
GO

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
GO

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
GO

CREATE TABLE [knowledge_map_associations] (
    [id] uniqueidentifier NOT NULL,
    [node_id] uniqueidentifier NOT NULL,
    [associated_type] int NOT NULL,
    [associated_id] uniqueidentifier NOT NULL,
    [order_index] int NOT NULL,
    CONSTRAINT [pk_knowledge_map_associations] PRIMARY KEY ([id])
);
GO

CREATE TABLE [knowledge_map_edges] (
    [id] uniqueidentifier NOT NULL,
    [map_id] uniqueidentifier NOT NULL,
    [from_node_id] uniqueidentifier NOT NULL,
    [to_node_id] uniqueidentifier NOT NULL,
    [relationship_type] int NOT NULL,
    [order_index] int NOT NULL,
    CONSTRAINT [pk_knowledge_map_edges] PRIMARY KEY ([id])
);
GO

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
GO

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
GO

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
GO

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
GO

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
GO

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
GO

CREATE TABLE [post_follows] (
    [id] uniqueidentifier NOT NULL,
    [post_id] uniqueidentifier NOT NULL,
    [user_id] uniqueidentifier NOT NULL,
    [followed_on] datetimeoffset NOT NULL,
    CONSTRAINT [pk_post_follows] PRIMARY KEY ([id])
);
GO

CREATE TABLE [post_ratings] (
    [id] uniqueidentifier NOT NULL,
    [post_id] uniqueidentifier NOT NULL,
    [user_id] uniqueidentifier NOT NULL,
    [stars] int NOT NULL,
    [rated_on] datetimeoffset NOT NULL,
    CONSTRAINT [pk_post_ratings] PRIMARY KEY ([id])
);
GO

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
GO

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
GO

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
GO

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
GO

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
GO

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
GO

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
GO

CREATE TABLE [topic_follows] (
    [id] uniqueidentifier NOT NULL,
    [topic_id] uniqueidentifier NOT NULL,
    [user_id] uniqueidentifier NOT NULL,
    [followed_on] datetimeoffset NOT NULL,
    CONSTRAINT [pk_topic_follows] PRIMARY KEY ([id])
);
GO

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
GO

CREATE TABLE [user_follows] (
    [id] uniqueidentifier NOT NULL,
    [follower_id] uniqueidentifier NOT NULL,
    [followed_id] uniqueidentifier NOT NULL,
    [followed_on] datetimeoffset NOT NULL,
    CONSTRAINT [pk_user_follows] PRIMARY KEY ([id])
);
GO

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
GO

CREATE TABLE [AspNetRoleClaims] (
    [id] int NOT NULL IDENTITY,
    [role_id] uniqueidentifier NOT NULL,
    [claim_type] nvarchar(max) NULL,
    [claim_value] nvarchar(max) NULL,
    CONSTRAINT [pk_asp_net_role_claims] PRIMARY KEY ([id]),
    CONSTRAINT [fk_asp_net_role_claims_asp_net_roles_role_id] FOREIGN KEY ([role_id]) REFERENCES [AspNetRoles] ([id]) ON DELETE CASCADE
);
GO

CREATE TABLE [AspNetUserClaims] (
    [id] int NOT NULL IDENTITY,
    [user_id] uniqueidentifier NOT NULL,
    [claim_type] nvarchar(max) NULL,
    [claim_value] nvarchar(max) NULL,
    CONSTRAINT [pk_asp_net_user_claims] PRIMARY KEY ([id]),
    CONSTRAINT [fk_asp_net_user_claims_asp_net_users_user_id] FOREIGN KEY ([user_id]) REFERENCES [AspNetUsers] ([id]) ON DELETE CASCADE
);
GO

CREATE TABLE [AspNetUserLogins] (
    [login_provider] nvarchar(450) NOT NULL,
    [provider_key] nvarchar(450) NOT NULL,
    [provider_display_name] nvarchar(max) NULL,
    [user_id] uniqueidentifier NOT NULL,
    CONSTRAINT [pk_asp_net_user_logins] PRIMARY KEY ([login_provider], [provider_key]),
    CONSTRAINT [fk_asp_net_user_logins_asp_net_users_user_id] FOREIGN KEY ([user_id]) REFERENCES [AspNetUsers] ([id]) ON DELETE CASCADE
);
GO

CREATE TABLE [AspNetUserRoles] (
    [user_id] uniqueidentifier NOT NULL,
    [role_id] uniqueidentifier NOT NULL,
    CONSTRAINT [pk_asp_net_user_roles] PRIMARY KEY ([user_id], [role_id]),
    CONSTRAINT [fk_asp_net_user_roles_asp_net_roles_role_id] FOREIGN KEY ([role_id]) REFERENCES [AspNetRoles] ([id]) ON DELETE CASCADE,
    CONSTRAINT [fk_asp_net_user_roles_asp_net_users_user_id] FOREIGN KEY ([user_id]) REFERENCES [AspNetUsers] ([id]) ON DELETE CASCADE
);
GO

CREATE TABLE [AspNetUserTokens] (
    [user_id] uniqueidentifier NOT NULL,
    [login_provider] nvarchar(450) NOT NULL,
    [name] nvarchar(450) NOT NULL,
    [value] nvarchar(max) NULL,
    CONSTRAINT [pk_asp_net_user_tokens] PRIMARY KEY ([user_id], [login_provider], [name]),
    CONSTRAINT [fk_asp_net_user_tokens_asp_net_users_user_id] FOREIGN KEY ([user_id]) REFERENCES [AspNetUsers] ([id]) ON DELETE CASCADE
);
GO

CREATE INDEX [ix_asp_net_role_claims_role_id] ON [AspNetRoleClaims] ([role_id]);
GO

CREATE UNIQUE INDEX [RoleNameIndex] ON [AspNetRoles] ([normalized_name]) WHERE [normalized_name] IS NOT NULL;
GO

CREATE INDEX [ix_asp_net_user_claims_user_id] ON [AspNetUserClaims] ([user_id]);
GO

CREATE INDEX [ix_asp_net_user_logins_user_id] ON [AspNetUserLogins] ([user_id]);
GO

CREATE INDEX [ix_asp_net_user_roles_role_id] ON [AspNetUserRoles] ([role_id]);
GO

CREATE INDEX [EmailIndex] ON [AspNetUsers] ([normalized_email]);
GO

CREATE INDEX [ix_users_country_id] ON [AspNetUsers] ([country_id]);
GO

CREATE UNIQUE INDEX [UserNameIndex] ON [AspNetUsers] ([normalized_user_name]) WHERE [normalized_user_name] IS NOT NULL;
GO

CREATE INDEX [ix_asset_file_scan_status] ON [asset_files] ([virus_scan_status]);
GO

CREATE INDEX [ix_city_result_scenario_at] ON [city_scenario_results] ([scenario_id], [computed_at]);
GO

CREATE INDEX [ix_city_scenario_user_modified] ON [city_scenarios] ([user_id], [last_modified_on]);
GO

CREATE INDEX [ix_city_tech_is_active] ON [city_technologies] ([is_active]);
GO

CREATE INDEX [ix_country_iso_alpha2] ON [countries] ([iso_alpha2]);
GO

CREATE UNIQUE INDEX [ux_country_iso_alpha3_active] ON [countries] ([iso_alpha3]) WHERE [is_deleted] = 0;
GO

CREATE INDEX [ix_kapsarc_snapshot_country_taken] ON [country_kapsarc_snapshots] ([country_id], [snapshot_taken_on]);
GO

CREATE UNIQUE INDEX [ux_country_profile_country_id] ON [country_profiles] ([country_id]);
GO

CREATE INDEX [ix_country_request_country_status] ON [country_resource_requests] ([country_id], [status]);
GO

CREATE INDEX [ix_event_starts_on] ON [events] ([starts_on]);
GO

CREATE UNIQUE INDEX [ux_event_ical_uid] ON [events] ([i_cal_uid]);
GO

CREATE UNIQUE INDEX [ux_expert_profile_active_user] ON [expert_profiles] ([user_id]) WHERE [is_deleted] = 0;
GO

CREATE INDEX [ix_expert_request_requested_by] ON [expert_registration_requests] ([requested_by_id]);
GO

CREATE INDEX [ix_expert_request_status] ON [expert_registration_requests] ([status]);
GO

CREATE INDEX [ix_homepage_section_active_order] ON [homepage_sections] ([is_active], [order_index]);
GO

CREATE UNIQUE INDEX [ux_km_assoc_node_type_id] ON [knowledge_map_associations] ([node_id], [associated_type], [associated_id]);
GO

CREATE INDEX [ix_km_edge_from_node] ON [knowledge_map_edges] ([from_node_id]);
GO

CREATE INDEX [ix_km_edge_to_node] ON [knowledge_map_edges] ([to_node_id]);
GO

CREATE UNIQUE INDEX [ux_km_edge_map_from_to_relation] ON [knowledge_map_edges] ([map_id], [from_node_id], [to_node_id], [relationship_type]);
GO

CREATE INDEX [ix_km_node_map_order] ON [knowledge_map_nodes] ([map_id], [order_index]);
GO

CREATE UNIQUE INDEX [ux_knowledge_map_slug_active] ON [knowledge_maps] ([slug]) WHERE [is_deleted] = 0;
GO

CREATE INDEX [ix_news_published_on] ON [news] ([published_on]);
GO

CREATE UNIQUE INDEX [ux_news_slug_active] ON [news] ([slug]) WHERE [is_deleted] = 0;
GO

CREATE INDEX [ix_newsletter_token] ON [newsletter_subscriptions] ([confirmation_token]);
GO

CREATE UNIQUE INDEX [ux_newsletter_email] ON [newsletter_subscriptions] ([email]);
GO

CREATE UNIQUE INDEX [ux_notification_template_code] ON [notification_templates] ([code]);
GO

CREATE UNIQUE INDEX [ux_page_type_slug_active] ON [pages] ([page_type], [slug]) WHERE [is_deleted] = 0;
GO

CREATE UNIQUE INDEX [ux_post_follow_post_user] ON [post_follows] ([post_id], [user_id]);
GO

CREATE UNIQUE INDEX [ux_post_rating_post_user] ON [post_ratings] ([post_id], [user_id]);
GO

CREATE INDEX [ix_post_reply_parent_id] ON [post_replies] ([parent_reply_id]);
GO

CREATE INDEX [ix_post_reply_post_id] ON [post_replies] ([post_id]);
GO

CREATE INDEX [ix_post_author_created] ON [posts] ([author_id], [created_on]);
GO

CREATE INDEX [ix_post_topic_id] ON [posts] ([topic_id]);
GO

CREATE INDEX [ix_resource_category_parent_id] ON [resource_categories] ([parent_id]);
GO

CREATE UNIQUE INDEX [ux_resource_category_slug] ON [resource_categories] ([slug]);
GO

CREATE INDEX [ix_resource_asset_file_id] ON [resources] ([asset_file_id]);
GO

CREATE INDEX [ix_resource_category_published] ON [resources] ([category_id], [published_on]);
GO

CREATE INDEX [ix_resource_country_id] ON [resources] ([country_id]);
GO

CREATE INDEX [ix_search_query_log_submitted_on] ON [search_query_logs] ([submitted_on]);
GO

CREATE INDEX [ix_service_rating_submitted_on] ON [service_ratings] ([submitted_on]);
GO

CREATE INDEX [ix_state_rep_country_id] ON [state_representative_assignments] ([country_id]);
GO

CREATE INDEX [ix_state_rep_user_id] ON [state_representative_assignments] ([user_id]);
GO

CREATE UNIQUE INDEX [ux_state_rep_active_user_country] ON [state_representative_assignments] ([user_id], [country_id]) WHERE [is_deleted] = 0;
GO

CREATE UNIQUE INDEX [ux_topic_follow_topic_user] ON [topic_follows] ([topic_id], [user_id]);
GO

CREATE UNIQUE INDEX [ux_topic_slug_active] ON [topics] ([slug]) WHERE [is_deleted] = 0;
GO

CREATE UNIQUE INDEX [ux_user_follow_follower_followed] ON [user_follows] ([follower_id], [followed_id]);
GO

CREATE INDEX [ix_user_notification_user_status] ON [user_notifications] ([user_id], [status]);
GO

INSERT INTO [__EFMigrationsHistory] ([migration_id], [product_version])
VALUES (N'20260427192344_DataDomainInitial', N'8.0.10');
GO

COMMIT;
GO

