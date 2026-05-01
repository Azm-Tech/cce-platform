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

  it('filters out invalid NodeType strings while keeping valid ones', () => {
    const state = parseUrlState({ type: 'Technology,InvalidType,Sector,SubTopic' });
    expect(state.filters).toEqual(['Technology', 'Sector', 'SubTopic']);
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

  it('serializes filters list to comma-separated string', () => {
    expect(buildUrlPatch({ filters: ['Technology', 'Sector'] }).type).toBe(
      'Technology,Sector',
    );
  });

  it('serializes open list to comma-separated string and clears when empty', () => {
    expect(buildUrlPatch({ open: ['a', 'b'] }).open).toBe('a,b');
    expect(buildUrlPatch({ open: [] }).open).toBeNull();
  });
});
