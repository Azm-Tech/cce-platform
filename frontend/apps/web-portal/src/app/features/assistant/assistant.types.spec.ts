import { newMessage, type ThreadMessage } from './assistant.types';

describe('assistant types helpers', () => {
  it('newMessage("user", text) produces a complete user message', () => {
    const m: ThreadMessage = newMessage('user', 'hello');
    expect(m.role).toBe('user');
    expect(m.content).toBe('hello');
    expect(m.status).toBe('complete');
    expect(m.citations).toEqual([]);
    expect(m.errorKind).toBeUndefined();
  });

  it('newMessage("assistant", "") produces a pending assistant placeholder', () => {
    const m: ThreadMessage = newMessage('assistant', '');
    expect(m.role).toBe('assistant');
    expect(m.content).toBe('');
    expect(m.status).toBe('pending');
  });

  it('newMessage assigns a unique id per call', () => {
    const a = newMessage('user', 'x');
    const b = newMessage('user', 'x');
    expect(a.id).not.toBe(b.id);
  });

  it('newMessage stamps an ISO 8601 createdAt', () => {
    const m = newMessage('user', 'x');
    expect(() => new Date(m.createdAt).toISOString()).not.toThrow();
    expect(m.createdAt.endsWith('Z')).toBe(true);
  });
});
