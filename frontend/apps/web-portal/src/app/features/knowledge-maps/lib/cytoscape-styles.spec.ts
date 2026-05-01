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

  it('defines a style for every NodeType (Technology, Sector, SubTopic)', () => {
    const selectors = sheet.map((e) => e.selector);
    expect(selectors).toContain('node[nodeType = "Technology"]');
    expect(selectors).toContain('node[nodeType = "Sector"]');
    expect(selectors).toContain('node[nodeType = "SubTopic"]');
  });

  it('defines a style for every RelationshipType (ParentOf, RelatedTo, RequiredBy)', () => {
    const selectors = sheet.map((e) => e.selector);
    expect(selectors).toContain('edge[relationshipType = "ParentOf"]');
    expect(selectors).toContain('edge[relationshipType = "RelatedTo"]');
    expect(selectors).toContain('edge[relationshipType = "RequiredBy"]');
  });

  it('defines selected + dimmed states for nodes and edges', () => {
    const selectors = sheet.map((e) => e.selector);
    expect(selectors).toContain('node:selected');
    expect(selectors).toContain('node.cce-dim');
    expect(selectors).toContain('edge.cce-dim');
  });
});
