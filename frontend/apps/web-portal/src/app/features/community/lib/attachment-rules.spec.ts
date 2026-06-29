import {
  ATTACHMENT_RULES,
  categorize,
  extensionOf,
  kindForCategory,
  validateAddition,
  type StagedLike,
} from './attachment-rules';

function file(name: string, sizeBytes = 1024, type = ''): File {
  const f = new File([new Uint8Array(0)], name, { type });
  // File size derives from content (0 here) — override so we can test byte caps.
  Object.defineProperty(f, 'size', { value: sizeBytes });
  return f;
}

const MB = 1024 * 1024;

describe('attachment-rules', () => {
  describe('extensionOf', () => {
    it('lowercases and strips the dot', () => {
      expect(extensionOf('Report.PDF')).toBe('pdf');
      expect(extensionOf('a.b.JPEG')).toBe('jpeg');
    });
    it('returns empty for no extension', () => {
      expect(extensionOf('noext')).toBe('');
      expect(extensionOf('trailing.')).toBe('');
    });
  });

  describe('categorize', () => {
    it('classifies every image extension', () => {
      for (const e of ['jpg', 'jpeg', 'png', 'webp']) {
        expect(categorize(`x.${e}`)).toBe('image');
      }
    });
    it('classifies every video extension', () => {
      for (const e of ['mp4', 'mov']) expect(categorize(`x.${e}`)).toBe('video');
    });
    it('classifies every file extension', () => {
      for (const e of ['pdf', 'doc', 'docx', 'ppt', 'pptx', 'xls', 'xlsx']) {
        expect(categorize(`x.${e}`)).toBe('file');
      }
    });
    it('is case-insensitive', () => {
      expect(categorize('PHOTO.JPG')).toBe('image');
    });
    it('rejects unknown extensions', () => {
      expect(categorize('virus.exe')).toBeNull();
      expect(categorize('archive.zip')).toBeNull();
      expect(categorize('noext')).toBeNull();
    });
    it('falls back to mimeType when extension is unknown', () => {
      expect(categorize('blob', 'image/png')).toBe('image');
      expect(categorize('blob', 'video/mp4')).toBe('video');
      expect(categorize('blob', 'application/pdf')).toBe('file');
    });
  });

  describe('kindForCategory', () => {
    it('maps image/video to 0 (inline media) and file to 1 (document)', () => {
      expect(kindForCategory('image')).toBe(0);
      expect(kindForCategory('video')).toBe(0);
      expect(kindForCategory('file')).toBe(1);
    });
  });

  describe('validateAddition', () => {
    it('accepts a valid image', () => {
      expect(validateAddition([], file('a.png', 2 * MB, 'image/png'))).toEqual({
        ok: true,
        category: 'image',
      });
    });

    it('rejects an unsupported type', () => {
      const r = validateAddition([], file('a.exe', 10));
      expect(r).toEqual({ ok: false, errorKey: 'community.compose.media.errorBadType' });
    });

    it('rejects an oversized image (>5MB)', () => {
      const r = validateAddition([], file('a.png', 6 * MB, 'image/png'));
      expect(r).toEqual({ ok: false, errorKey: 'community.compose.media.errorTooLarge' });
    });

    it('rejects an oversized video (>100MB) and oversized file (>10MB)', () => {
      expect(validateAddition([], file('v.mp4', 101 * MB, 'video/mp4'))).toEqual({
        ok: false,
        errorKey: 'community.compose.media.errorTooLarge',
      });
      expect(validateAddition([], file('d.pdf', 11 * MB, 'application/pdf'))).toEqual({
        ok: false,
        errorKey: 'community.compose.media.errorTooLarge',
      });
    });

    it('enforces the 5-image cap', () => {
      const existing: StagedLike[] = Array.from({ length: 5 }, () => ({ category: 'image' as const }));
      const r = validateAddition(existing, file('f.jpg', 1 * MB, 'image/jpeg'));
      expect(r).toEqual({ ok: false, errorKey: 'community.compose.media.errorTooMany' });
    });

    it('enforces the single-video cap with a dedicated message', () => {
      const existing: StagedLike[] = [{ category: 'video' }];
      const r = validateAddition(existing, file('v2.mov', 1 * MB, 'video/quicktime'));
      expect(r).toEqual({ ok: false, errorKey: 'community.compose.media.errorVideoExists' });
    });

    it('enforces the 3-file cap', () => {
      const existing: StagedLike[] = Array.from({ length: 3 }, () => ({ category: 'file' as const }));
      const r = validateAddition(existing, file('d.docx', 1 * MB));
      expect(r).toEqual({ ok: false, errorKey: 'community.compose.media.errorTooMany' });
    });

    it('counts categories independently', () => {
      const existing: StagedLike[] = [
        { category: 'image' },
        { category: 'image' },
        { category: 'file' },
      ];
      // image still has room (2/5), video empty, file has room (1/3)
      expect(validateAddition(existing, file('c.png', 1 * MB, 'image/png')).ok).toBe(true);
      expect(validateAddition(existing, file('c.mp4', 1 * MB, 'video/mp4')).ok).toBe(true);
      expect(validateAddition(existing, file('c.pdf', 1 * MB, 'application/pdf')).ok).toBe(true);
    });
  });

  it('exposes the documented limits', () => {
    expect(ATTACHMENT_RULES.image.maxCount).toBe(5);
    expect(ATTACHMENT_RULES.video.maxCount).toBe(1);
    expect(ATTACHMENT_RULES.file.maxCount).toBe(3);
    expect(ATTACHMENT_RULES.image.maxBytes).toBe(5 * MB);
    expect(ATTACHMENT_RULES.video.maxBytes).toBe(100 * MB);
    expect(ATTACHMENT_RULES.file.maxBytes).toBe(10 * MB);
  });
});
