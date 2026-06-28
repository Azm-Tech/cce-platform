import { buildUrlPatch, parseUrlState } from './url-state';

describe('parseUrlState', () => {
  it('returns sensible defaults for empty params', () => {
    const state = parseUrlState({});
    expect(state.open).toEqual([]);
    expect(state.node).toBeNull();
    expect(state.q).toBe('');
    expect(state.filters).toEqual([]);
    expect(state.view).toBe('graph');
  });

  it('parses comma-separated open ids and trims whitespace', () => {
    const state = parseUrlState({ open: 'a, b ,c' });
    expect(state.open).toEqual(['a', 'b', 'c']);
  });

  it('filters out invalid level values while keeping valid 0, 1, 2', () => {
    const state = parseUrlState({ type: '0,5,1,invalid,2' });
    expect(state.filters).toEqual([0, 1, 2]);
  });

  it('parses all three valid levels', () => {
    const state = parseUrlState({ type: '0,1,2' });
    expect(state.filters).toEqual([0, 1, 2]);
  });

  it('falls back to view=graph when ?view= is missing or invalid', () => {
    expect(parseUrlState({}).view).toBe('graph');
    expect(parseUrlState({ view: 'something-else' }).view).toBe('graph');
    expect(parseUrlState({ view: 'list' }).view).toBe('list');
  });

  it('captures node id from ?node=', () => {
    expect(parseUrlState({ node: 'n42' }).node).toBe('n42');
    expect(parseUrlState({}).node).toBeNull();
  });
});

describe('buildUrlPatch', () => {
  it('clears the type param to null when filters is empty', () => {
    expect(buildUrlPatch({ filters: [] }).type).toBeNull();
  });

  it('clears the view param to null when default (graph)', () => {
    expect(buildUrlPatch({ view: 'graph' }).view).toBeNull();
    expect(buildUrlPatch({ view: 'list' }).view).toBe('list');
  });

  it('clears q to null when empty', () => {
    expect(buildUrlPatch({ q: '' }).q).toBeNull();
    expect(buildUrlPatch({ q: 'carbon' }).q).toBe('carbon');
  });

  it('serializes level filters list to comma-separated string', () => {
    expect(buildUrlPatch({ filters: [0, 1] }).type).toBe('0,1');
    expect(buildUrlPatch({ filters: [2] }).type).toBe('2');
  });

  it('serializes open list to comma-separated string and clears when empty', () => {
    expect(buildUrlPatch({ open: ['a', 'b'] }).open).toBe('a,b');
    expect(buildUrlPatch({ open: [] }).open).toBeNull();
  });
});
