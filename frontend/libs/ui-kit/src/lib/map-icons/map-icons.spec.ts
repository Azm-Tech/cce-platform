import { MAP_ICON_KEYS, iconDataUri, isCustomIconUrl } from './map-icons';

describe('map-icons', () => {
  it('exposes the library keys without the internal fallback', () => {
    expect(MAP_ICON_KEYS.length).toBeGreaterThan(30);
    expect(MAP_ICON_KEYS).toContain('co2');
    expect(MAP_ICON_KEYS).toContain('factory');
    expect(MAP_ICON_KEYS).not.toContain('__fallback');
  });

  it('isCustomIconUrl detects URLs / data / blob, not registry keys', () => {
    expect(isCustomIconUrl('https://cdn.example.com/i.svg')).toBe(true);
    expect(isCustomIconUrl('//cdn/i.png')).toBe(true);
    expect(isCustomIconUrl('/api/admin/assets/abc')).toBe(true);
    expect(isCustomIconUrl('data:image/svg+xml;utf8,<svg/>')).toBe(true);
    expect(isCustomIconUrl('blob:http://x/y')).toBe(true);
    expect(isCustomIconUrl('factory')).toBe(false);
    expect(isCustomIconUrl('')).toBe(false);
    expect(isCustomIconUrl(null)).toBe(false);
  });

  it('iconDataUri returns an uploaded URL verbatim (passthrough)', () => {
    const url = 'https://cdn.example.com/custom-icon.png';
    expect(iconDataUri(url)).toBe(url);
    expect(iconDataUri('/api/admin/assets/123')).toBe('/api/admin/assets/123');
  });

  it('iconDataUri builds an inline SVG data-URI for a registry key', () => {
    const out = iconDataUri('factory');
    expect(out.startsWith('data:image/svg+xml')).toBe(true);
    expect(out).toContain('svg');
  });

  it('iconDataUri honours a stroke override', () => {
    const out = decodeURIComponent(iconDataUri('co2', { stroke: '#123456' }));
    expect(out).toContain('stroke="#123456"');
  });

  it('unknown key falls back (still a valid data-URI)', () => {
    expect(iconDataUri('totally-unknown').startsWith('data:image/svg+xml')).toBe(true);
  });
});
