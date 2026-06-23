/**
 * Real-time notifications hub contract — mirrors the SignalR `NotificationsHub`
 * registered on the External API (`/hubs/notifications`).
 *
 * Payload property names: the hub mixes PascalCase (named records) and camelCase
 * (anonymous objects). All payloads pass through `normalizePayload` before reaching
 * these streams, so the interfaces below are declared in canonical **camelCase**.
 */

/** Exact server→client event names passed to `connection.on(...)`. */
export const RealtimeEvent = {
  /** post:{postId} — a reply was added. */
  NewReply: 'NewReply',
  /** post:{postId} — a post or reply vote changed (distinguish by which id is present). */
  VoteChanged: 'VoteChanged',
  /** post:{postId} — poll tallies changed; IDs only, refetch results. */
  PollResultsChanged: 'PollResultsChanged',
  /** post:{postId} / community:{communityId} — a post or reply was soft-deleted. */
  PostModerated: 'PostModerated',
  /** post:{postId} — live distinct-viewer count. */
  PresenceChanged: 'PresenceChanged',
  /** post:{postId} — someone started/stopped typing (sent to others only). */
  TypingChanged: 'TypingChanged',
  /** community:{communityId} / topic:{topicId} — a new post was published. */
  NewPost: 'NewPost',
  /** user:{userId} — a personal notification (bell). Payload opaque; refetch count. */
  ReceiveNotification: 'ReceiveNotification',
  /** moderation — content was moderated (admins). */
  ContentModerated: 'ContentModerated',
} as const;

export type RealtimeEventName = (typeof RealtimeEvent)[keyof typeof RealtimeEvent];

// ── Payloads (post:{postId}) ───────────────────────────────────────────────
export interface NewReplyPayload {
  postId: string;
  replyId: string;
  parentReplyId: string | null;
  depth: number;
}

/** `postId` present → post vote; `replyId` present → reply vote. */
export interface VoteChangedPayload {
  postId?: string;
  replyId?: string;
  upvoteCount: number;
  score: number;
}

export interface PollResultsChangedPayload {
  pollId: string;
  postId: string;
}

/** `replyId` null when the post itself was deleted. `action` e.g. "SoftDeleted". */
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

// ── Payloads (community:{communityId} / topic:{topicId}) ────────────────────
export interface NewPostPayload {
  postId: string;
  communityId: string;
  topicId: string;
  authorId: string;
  publishedOn: string;
}

// ── Payloads (moderation) ───────────────────────────────────────────────────
export interface ContentModeratedPayload {
  contentType: 'Post' | 'Reply';
  contentId: string;
  postId: string;
  moderatorId: string;
  action: string;
}

/** user:{userId} `ReceiveNotification` — shape unconfirmed; treat as opaque and refetch. */
export type ReceiveNotificationPayload = Record<string, unknown>;

export type RealtimeConnectionState =
  | 'disconnected'
  | 'connecting'
  | 'connected'
  | 'reconnecting';
