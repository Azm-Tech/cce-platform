import type { PagedResult } from '../identity/identity.types';

export interface AuditEvent {
  id: string;
  occurredOn: string;
  actor: string;
  action: string;
  resource: string;
  correlationId: string;
  diff: string | null;
}

export type { PagedResult };
