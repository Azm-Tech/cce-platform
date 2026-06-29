import type { PagedResult } from '../knowledge-center/shared.types';

// ── Enums ──────────────────────────────────────────────────────────────────────
/** 0 = informational, 1 = question/answerable, 2 = poll */
export type PostType = 0 | 1 | 2;

/** 1 = upvote, -1 = downvote, 0 = remove/neutral */
export type VoteDirection = 1 | -1 | 0;

/** 0 = inline media (image/video), 1 = document */
export type AttachmentKind = 0 | 1;

/** 0 = public, 1 = private */
export type CommunityVisibility = 0 | 1;

// ── Community ─────────────────────────────────────────────────────────────────
export interface CommunityDto {
  id: string;
  nameAr: string | null;
  nameEn: string | null;
  descriptionAr: string | null;
  descriptionEn: string | null;
  slug: string | null;
  visibility: CommunityVisibility;
  memberCount: number;
  presentationJson: string | null;
}

// ── Roles ───────────────────────────────────────────────────────────────────────
/** Community role definition from GET /api/community/roles. */
export interface CommunityRole {
  key: string;
  nameAr: string | null;
  nameEn: string | null;
  descriptionAr: string | null;
  descriptionEn: string | null;
  capabilities: string[];
}

// ── Topic ─────────────────────────────────────────────────────────────────────
export interface PublicTopic {
  id: string;
  nameAr: string | null;
  nameEn: string | null;
  descriptionAr: string | null;
  descriptionEn: string | null;
  slug: string | null;
  parentId: string | null;
  iconUrl: string | null;
  orderIndex: number;
}

/** Lightweight topic shape from GET /api/community/topics (includes post counts + follow state). */
export interface CommunityTopicSummary {
  id: string;
  nameAr: string | null;
  nameEn: string | null;
  postsCount: number;
  /** Whether the current user follows this topic (authenticated responses). */
  isFollowed?: boolean;
}

// ── Post ──────────────────────────────────────────────────────────────────────
export interface PostAttachment {
  id: string;
  assetFileId: string;
  kind: AttachmentKind;
  sortOrder: number;
  url: string | null;
  fileName: string | null;
  fileSizeBytes: number | null;
  metadataJson: string | null;
}

/** Resolved post media returned (with public URL) by the post detail endpoint. */
export interface PostMedia {
  assetFileId: string;
  /** Server category: "media" (image/video) or "document". */
  kind: string;
  mimeType: string | null;
  url: string;
  sizeBytes: number | null;
  originalFileName: string | null;
  sortOrder: number;
}

export interface PostAuthor {
  id: string;
  name: string | null;
  avatarUrl: string | null;
  isExpert: boolean;
  postsCount: number;
  followerCount: number;
  /** Optional profile details — rendered when the API includes them. */
  jobTitle?: string | null;
  organizationName?: string | null;
}

export interface PublicPost {
  id: string;
  communityId: string;
  topicId: string;
  /** Feed (`/api/community/feed`) returns flat author fields. */
  authorId: string;
  authorName: string | null;
  /** Post detail (`/api/community/posts/{id}`) nests the author here instead. */
  author?: PostAuthor | null;
  type: string;
  title: string | null;
  content: string | null;
  locale: string;
  isAnswerable: boolean;
  answeredReplyId: string | null;
  upvoteCount: number;
  downvoteCount: number;
  commentsCount: number;
  attachmentIds: string[];
  /** Full attachment objects — populated by the post detail endpoint. */
  attachments?: PostAttachment[] | null;
  /** Resolved media objects (with public URLs) — post detail endpoint. */
  media?: PostMedia[] | null;
  /** First image's public URL — feed/list endpoint, shown as the post thumbnail. */
  mainImageUrl?: string | null;
  tagIds?: string[];
  createdOn: string;
  topicNameAr: string | null;
  topicNameEn: string | null;
  isExpert: boolean;
  isWatchlisted: boolean;
  /** Whether the current user follows this post (post detail endpoint). */
  isFollowed: boolean;
  voteStatus: number;
  /** Inline poll for `type === 'Poll'` posts — rendered when the API includes it. */
  poll?: PostPoll | null;
}

/** Poll embedded in a poll post's detail response. */
export interface PostPoll {
  pollId: string;
  deadline: string | null;
  isClosed: boolean;
  allowMultiple: boolean;
  isAnonymous: boolean;
  resultsVisible: boolean;
  totalVotes: number;
  /** Option ids the current user has already voted for. */
  myVotedOptionIds: string[] | null;
  options: PollOptionResult[] | null;
}

// ── Reply ─────────────────────────────────────────────────────────────────────
export interface PublicPostReply {
  id: string;
  postId: string;
  authorId: string;
  authorName: string | null;
  /** Added in the mentions sprint — may be absent on older API versions. */
  authorAvatarUrl?: string | null;
  content: string | null;
  locale: string | null;
  parentReplyId: string | null;
  isByExpert: boolean;
  depth: number;
  childCount: number;
  upvoteCount: number;
  createdOn: string;
  /** Ordered by first occurrence in `content`. Used for positional @mention
   *  resolution — the nth @token maps to the nth unique user in this array. */
  mentionedUsers?: MentionUser[] | null;
}

// ── Poll ──────────────────────────────────────────────────────────────────────
export interface PollOptionResult {
  id: string;
  label: string | null;
  voteCount: number;
  percentage: number;
}

export interface PollResults {
  pollId: string;
  deadline: string;
  isClosed: boolean;
  allowMultiple: boolean;
  resultsVisible: boolean;
  totalVotes: number;
  options: PollOptionResult[] | null;
}

/** Reconnect catch-up delta for a post — `GET /api/community/posts/{id}/activity?since=`. */
export interface PostActivity {
  upvoteCount: number;
  downvoteCount: number;
  score: number;
  replyCount: number;
  newReplies: PublicPostReply[] | null;
  poll: PollResults | null;
}

// ── Community User ────────────────────────────────────────────────────────────
export interface CommunityUserProfile {
  userId: string;
  firstName: string | null;
  lastName: string | null;
  jobTitle: string | null;
  organizationName: string | null;
  avatarUrl: string | null;
  isExpert: boolean;
  postCount: number;
  replyCount: number;
  followerCount: number;
  followingCount: number;
  /** Localized expert biography ("السيرة الذاتية"). */
  expertBioAr?: string | null;
  expertBioEn?: string | null;
  isFollowed?: boolean;
  countryNameAr?: string | null;
  countryNameEn?: string | null;
  bio?: string | null;
  location?: string | null;
  joinedOn?: string | null;
  joinedDate?: string | null;
}

// ── Featured Post ─────────────────────────────────────────────────────────────
export interface FeaturedPost {
  id: string;
  topicId: string;
  nameAr: string | null;
  nameEn: string | null;
  content: string | null;
  locale: string | null;
  authorId: string;
  publishedByName: string | null;
  publishedOn: string;
  ratingCount: number;
  averageStars: number;
  replyCount: number;
}

// ── Post Share ────────────────────────────────────────────────────────────────
export interface PostShareLink {
  postId: string;
  url: string | null;
}

// ── Payloads ──────────────────────────────────────────────────────────────────
export interface PollInputPayload {
  deadline: string;
  allowMultiple: boolean;
  isAnonymous: boolean;
  showResultsBeforeClose: boolean;
  optionLabels: string[];
}

export interface PostAttachmentPayload {
  assetFileId: string;
  kind: AttachmentKind;
  sortOrder: number;
  metadataJson?: string | null;
  /** Original file MIME type — used by the API for server-side validation. */
  mimeType?: string | null;
  /** Original file size in bytes — used by the API for server-side validation. */
  sizeBytes?: number | null;
}

export interface CreatePostPayload {
  communityId: string;
  topicId: string;
  type: PostType;
  title?: string | null;
  content?: string | null;
  locale: string;
  isAnswerable?: boolean | null;
  tagIds?: string[] | null;
  attachments?: PostAttachmentPayload[] | null;
  poll?: PollInputPayload | null;
  saveAsDraft: boolean;
}

export interface UpdateDraftPayload {
  topicId?: string;
  type?: PostType;
  title?: string | null;
  content?: string | null;
  locale?: string;
  tagIds?: string[] | null;
  attachments?: PostAttachmentPayload[] | null;
  poll?: PollInputPayload | null;
}

export interface CreateReplyPayload {
  content: string;
  locale: string;
  parentReplyId?: string | null;
}

export interface EditReplyPayload {
  content: string;
}

/** Participant available for @mention in the reply composer,
 *  and the shape returned inside `PublicPostReply.mentionedUsers`. */
export interface MentionUser {
  id: string;
  name: string;
  avatarUrl: string | null;
  /** Available when the backend includes it — used for the popup expert badge
   *  before the full profile is fetched. */
  isExpert?: boolean;
}

/** Shape returned by GET /api/community/communities/{id}/mentionable-users */
export interface MentionableUser {
  userId: string;
  displayName: string;
  avatarUrl: string | null;
  isFollowed: boolean;
  isMember: boolean;
}

/** One item from GET /api/me/comments — a comment the current user authored. */
/** One item from GET /api/me/replies — a reply/comment the current user authored. */
export interface MyCommentItem {
  /** The reply/comment id — used as the in-post anchor "reply-{replyId}". */
  replyId: string;
  postId: string;
  postTitle: string;
  /** The reply author (the current user). */
  authorId: string;
  authorName: string | null;
  content: string;
  upvoteCount: number;
  downvoteCount: number;
  createdOn: string;
}

export interface MarkAnswerPayload {
  replyId: string;
}

export type { PagedResult };
