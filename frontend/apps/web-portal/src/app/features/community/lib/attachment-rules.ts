/**
 * Community post attachment rules — the single source of truth for media
 * validation (counts, sizes, extensions) shared by the compose form and tests.
 *
 * The backend stores an attachment `kind`: 0 = inline media (image/video),
 * 1 = document. The UI further splits media into image vs video so it can
 * enforce the per-type caps below and pick the right preview/player.
 */

export type MediaCategory = 'image' | 'video' | 'file';

export interface CategoryRule {
  category: MediaCategory;
  /** Max number of this category allowed per post. */
  maxCount: number;
  /** Max bytes per single file. */
  maxBytes: number;
  /** Allowed extensions (lowercase, no dot). */
  extensions: readonly string[];
  /** Backend attachment kind: 0 = inline media, 1 = document. */
  kind: 0 | 1;
}

const MB = 1024 * 1024;

export const ATTACHMENT_RULES: Record<MediaCategory, CategoryRule> = {
  image: {
    category: 'image',
    maxCount: 5,
    maxBytes: 5 * MB,
    extensions: ['jpg', 'jpeg', 'png', 'webp'],
    kind: 0,
  },
  video: {
    category: 'video',
    maxCount: 1,
    maxBytes: 100 * MB,
    extensions: ['mp4', 'mov'],
    kind: 0,
  },
  file: {
    category: 'file',
    maxCount: 3,
    maxBytes: 10 * MB,
    extensions: ['pdf', 'doc', 'docx', 'ppt', 'pptx', 'xls', 'xlsx'],
    kind: 1,
  },
};

export const MEDIA_CATEGORIES: readonly MediaCategory[] = ['image', 'video', 'file'];

/** Lowercased extension without the dot; '' when the name has none. */
export function extensionOf(name: string): string {
  const dot = name.lastIndexOf('.');
  if (dot < 0 || dot === name.length - 1) return '';
  return name.slice(dot + 1).toLowerCase();
}

/** mimeType backstop — maps a leading mime group/value to a category. */
function categoryFromMime(mimeType?: string): MediaCategory | null {
  if (!mimeType) return null;
  const m = mimeType.toLowerCase();
  if (m.startsWith('image/')) return 'image';
  if (m.startsWith('video/')) return 'video';
  if (
    m === 'application/pdf' ||
    m.includes('word') ||
    m.includes('presentation') ||
    m.includes('powerpoint') ||
    m.includes('excel') ||
    m.includes('spreadsheet') ||
    m.includes('officedocument')
  ) {
    return 'file';
  }
  return null;
}

/**
 * Classify a file by extension first (per spec), falling back to mimeType.
 * Returns null for anything outside the allowed sets.
 */
export function categorize(fileName: string, mimeType?: string): MediaCategory | null {
  const ext = extensionOf(fileName);
  for (const cat of MEDIA_CATEGORIES) {
    if (ATTACHMENT_RULES[cat].extensions.includes(ext)) return cat;
  }
  // Extension unknown/missing — try the mime type, but only honour it if its
  // category genuinely accepts this file (keeps unknown types rejected).
  const byMime = categoryFromMime(mimeType);
  return byMime;
}

export function kindForCategory(cat: MediaCategory): 0 | 1 {
  return ATTACHMENT_RULES[cat].kind;
}

export interface StagedLike {
  category: MediaCategory;
}

export type AdditionResult =
  | { ok: true; category: MediaCategory }
  | { ok: false; errorKey: string };

const ERR_BAD_TYPE = 'community.compose.media.errorBadType';
const ERR_TOO_LARGE = 'community.compose.media.errorTooLarge';
const ERR_TOO_MANY = 'community.compose.media.errorTooMany';
const ERR_VIDEO_EXISTS = 'community.compose.media.errorVideoExists';

/**
 * Validate adding `file` to the already-staged list. Enforces, per category:
 * allowed extension, max bytes, and max count. Call sequentially when adding a
 * batch so the running `existing` count is honoured.
 */
export function validateAddition(
  existing: readonly StagedLike[],
  file: File,
): AdditionResult {
  const category = categorize(file.name, file.type);
  if (!category) return { ok: false, errorKey: ERR_BAD_TYPE };

  const rule = ATTACHMENT_RULES[category];
  if (file.size > rule.maxBytes) return { ok: false, errorKey: ERR_TOO_LARGE };

  const count = existing.filter((e) => e.category === category).length;
  if (count >= rule.maxCount) {
    return {
      ok: false,
      errorKey: category === 'video' ? ERR_VIDEO_EXISTS : ERR_TOO_MANY,
    };
  }
  return { ok: true, category };
}
