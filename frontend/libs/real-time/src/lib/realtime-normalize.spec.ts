import { normalizePayload } from './realtime-normalize';
import type { NewReplyPayload, VoteChangedPayload } from './realtime.types';

describe('normalizePayload', () => {
  it('lower-cases PascalCase top-level keys', () => {
    const raw = { PostId: 'p1', ReplyId: 'r1', ParentReplyId: null, Depth: 2 };
    expect(normalizePayload<NewReplyPayload>(raw)).toEqual({
      postId: 'p1',
      replyId: 'r1',
      parentReplyId: null,
      depth: 2,
    });
  });

  it('leaves camelCase keys unchanged', () => {
    const raw = { postId: 'p1', upvoteCount: 3, score: 5 };
    expect(normalizePayload<VoteChangedPayload>(raw)).toEqual({
      postId: 'p1',
      upvoteCount: 3,
      score: 5,
    });
  });

  it('prefers an existing camelCase key over a PascalCase duplicate', () => {
    const raw = { postId: 'camel', PostId: 'pascal' };
    expect(normalizePayload<{ postId: string }>(raw).postId).toBe('camel');
  });

  it('passes through non-objects untouched', () => {
    expect(normalizePayload<string>('x')).toBe('x');
    expect(normalizePayload<null>(null)).toBeNull();
  });
});
