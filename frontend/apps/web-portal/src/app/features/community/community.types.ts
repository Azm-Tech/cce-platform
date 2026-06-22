import type { PagedResult } from '../knowledge-center/shared.types';

// ── Enums ──────────────────────────────────────────────────────────────────────
/** 0 = informational, 1 = question/answerable, 2 = poll */
export type PostType = 0 | 1 | 2;

/** 1 = upvote, -1 = downvote, 0 = remove/neutral */
export type VoteDirection = 1 | -1 | 0;

/** 0 = document, 1 = image */
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

/** Lightweight topic shape from GET /api/community/topics (includes post counts). */
export interface CommunityTopicSummary {
  id: string;
  nameAr: string | null;
  nameEn: string | null;
  postsCount: number;
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
  content: string | null;
  locale: string | null;
  parentReplyId: string | null;
  isByExpert: boolean;
  depth: number;
  childCount: number;
  upvoteCount: number;
  createdOn: string;
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
  mentionedUserIds?: string[] | null;
}

export interface EditReplyPayload {
  content: string;
}

export interface MarkAnswerPayload {
  replyId: string;
}

export type { PagedResult };
