/**
 * Real-time notifications hub contract — mirrors the SignalR `NotificationsHub`
 * (`/hubs/notifications`). Authoritative contract from the backend team.
 *
 * Every push is wrapped in a {@link RealtimeEnvelope}; `RealtimeHubService`
 * unwraps `.payload` (deduping on `.eventId`) before emitting to streams, so the
 * payload interfaces below describe the UNWRAPPED shape. Property names are
 * camelCase; payloads still pass through `normalizePayload` defensively.
 */

/** Wrapper around every server→client push. */
export interface RealtimeEnvelope<T = unknown> {
  /** Random GUID for deduplication (not monotonic). */
  eventId: string;
  /** ISO 8601 timestamp — event ordering + reconnect catch-up cursor. */
  occurredOn: string;
  payload: T;
}

/** Exact, case-sensitive server→client event names passed to `connection.on(...)`. */
export const RealtimeEvent = {
  /** user:{userId} — personal notification (bell). */
  ReceiveNotification: 'ReceiveNotification',
  /** post:{postId} — a reply was posted. */
  NewReply: 'NewReply',
  /** post:{postId} — a post or reply vote count changed (distinguish by which id is present). */
  VoteChanged: 'VoteChanged',
  /** post:{postId} — a poll vote was cast (results inline). */
  PollResultsChanged: 'PollResultsChanged',
  /** community:{communityId} / topic:{topicId} — a post was published. */
  NewPost: 'NewPost',
  /** post:{postId} / community:{communityId} — a post or reply was soft-deleted. */
  PostModerated: 'PostModerated',
  /** moderation — moderator action alert (moderation queue screens only). */
  ContentModerated: 'ContentModerated',
  /** post:{postId} — live distinct-viewer count. */
  PresenceChanged: 'PresenceChanged',
  /** post:{postId} — someone started/stopped typing (not echoed to sender). */
  TypingChanged: 'TypingChanged',
} as const;

export type RealtimeEventName = (typeof RealtimeEvent)[keyof typeof RealtimeEvent];

// ── user:{userId} ───────────────────────────────────────────────────────────
export interface ReceiveNotificationPayload {
  id: string;
  templateId: string | null;
  renderedSubjectAr: string | null;
  renderedSubjectEn: string | null;
  renderedBody: string | null;
  renderedLocale: string | null;
  status: string | null;
  sentOn: string | null;
  actorId: string | null;
  /** Deep-link target ids. */
  metaData?: { postId?: string | null; replyId?: string | null } | null;
}

// ── post:{postId} ────────────────────────────────────────────────────────────
export interface NewReplyPayload {
  replyId: string;
  postId: string;
  parentReplyId: string | null;
  depth: number;
  body: string | null;
  createdOn: string;
  author: { id: string; name: string | null; avatarUrl: string | null } | null;
}

/** `postId` present → post vote; `replyId` present → reply vote. */
export interface VoteChangedPayload {
  postId?: string;
  replyId?: string;
  upvoteCount: number;
  downvoteCount: number;
  score: number;
}

export interface RealtimePollOption {
  id: string;
  voteCount: number;
  percentage: number;
}

export interface PollResultsChangedPayload {
  pollId: string;
  postId: string;
  totalVotes: number;
  options: RealtimePollOption[] | null;
}

/** `replyId` null = the post itself was moderated. `action` e.g. "SoftDeleted". */
export interface PostModeratedPayload {
  postId: string;
  replyId: string | null;
  action: string;
}

export interface PresenceChangedPayload {
  postId: string;
  viewers: number;
}

export interface TypingChangedPayload {
  postId: string;
  userId: string;
  isTyping: boolean;
}

// ── community:{communityId} / topic:{topicId} ───────────────────────────────
export interface NewPostPayload {
  postId: string;
  communityId: string;
  topicId: string;
  authorId: string;
  publishedOn: string;
  /** Provided for toast rendering; full card still needs an API fetch. */
  title: string | null;
}

// ── moderation ───────────────────────────────────────────────────────────────
export interface ContentModeratedPayload {
  contentType: 'Post' | 'Reply';
  contentId: string;
  postId: string;
  moderatorId: string;
  action: string;
}

export type RealtimeConnectionState =
  | 'disconnected'
  | 'connecting'
  | 'connected'
  | 'reconnecting';
