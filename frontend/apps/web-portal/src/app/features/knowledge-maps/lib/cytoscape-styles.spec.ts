import { buildStylesheet } from './cytoscape-styles';

describe('buildStylesheet', () => {
  const sheet = buildStylesheet();

  it('returns a non-empty array of Cytoscape style entries', () => {
    expect(Array.isArray(sheet)).toBe(true);
    expect(sheet.length).toBeGreaterThan(0);
    sheet.forEach((entry) => {
      expect(typeof entry.selector).toBe('string');
      // StylesheetJsonBlock = StylesheetStyle | StylesheetCSS — either
      // `style` or `css` carries the rules. Our builder uses `style`.
      const block = entry as { style?: object; css?: object };
      expect(typeof (block.style ?? block.css)).toBe('object');
    });
  });

  it('defines a style for every level (0 = Root, 1 = Category, 2 = Topic)', () => {
    const selectors = sheet.map((e) => e.selector);
    expect(selectors).toContain('node[level = 0]');
    expect(selectors).toContain('node[level = 1]');
    expect(selectors).toContain('node[level = 2]');
  });

  it('defines a base edge style (all edges are parent-child)', () => {
    const selectors = sheet.map((e) => e.selector);
    expect(selectors).toContain('edge');
  });

  it('defines selected + dimmed states for nodes and edges', () => {
    const selectors = sheet.map((e) => e.selector);
    expect(selectors).toContain('node:selected');
    expect(selectors).toContain('node.cce-dim');
    expect(selectors).toContain('edge.cce-dim');
  });
});
