export * from './lib/api-client/api-client.component';

// Generated OpenAPI TYPES (no runtime client) from contracts/openapi.{external,internal}.json
// Regenerate via: pnpm nx run api-client:generate-types
// Curated aliases (ApiEnvelope, PublicResourceDto, …) live in lib/schema/index.ts
export * from './lib/schema';

// DEPRECATED: @hey-api runtime clients — bypass interceptors; do not use in new code.
// Kept until existing imports are migrated (see technical/API_INTEGRATION.md).
// Regenerate via: pnpm nx run api-client:generate
export * as ExternalApi from './lib/generated/external';
export * as InternalApi from './lib/generated/internal';
