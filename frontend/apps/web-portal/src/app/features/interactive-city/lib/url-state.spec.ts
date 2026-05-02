import { convertToParamMap } from '@angular/router';
import { buildUrlPatch, parseUrlState } from './url-state';

const VALID_GUID_A = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa';
const VALID_GUID_B = 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb';
const FIXED_NOW = new Date('2026-05-02T00:00:00Z');

describe('parseUrlState', () => {
  it('returns defaults for empty params', () => {
    const s = parseUrlState(convertToParamMap({}), FIXED_NOW);
    expect(s).toEqual({
      cityType: 'Mixed',
      targetYear: 2030,
      selectedIds: [],
      name: '',
    });
  });

  it('parses a fully populated URL', () => {
    const s = parseUrlState(
      convertToParamMap({
        city: 'Industrial',
        year: '2035',
        t: `${VALID_GUID_A},${VALID_GUID_B}`,
        name: 'My City',
      }),
      FIXED_NOW,
    );
    expect(s).toEqual({
      cityType: 'Industrial',
      targetYear: 2035,
      selectedIds: [VALID_GUID_A, VALID_GUID_B],
      name: 'My City',
    });
  });

  it('falls back to default city when value is unknown', () => {
    const s = parseUrlState(convertToParamMap({ city: 'NotACity' }), FIXED_NOW);
    expect(s.cityType).toBe('Mixed');
  });

  it('falls back to default year when value is non-numeric', () => {
    const s = parseUrlState(convertToParamMap({ year: 'soon' }), FIXED_NOW);
    expect(s.targetYear).toBe(2030);
  });

  it('falls back to default year when value is out of bounds', () => {
    const sLow = parseUrlState(convertToParamMap({ year: '1999' }), FIXED_NOW);
    const sHigh = parseUrlState(convertToParamMap({ year: '3000' }), FIXED_NOW);
    expect(sLow.targetYear).toBe(2030);
    expect(sHigh.targetYear).toBe(2030);
  });

  it('drops non-GUID t entries', () => {
    const s = parseUrlState(
      convertToParamMap({ t: `${VALID_GUID_A},not-a-guid,${VALID_GUID_B}` }),
      FIXED_NOW,
    );
    expect(s.selectedIds).toEqual([VALID_GUID_A, VALID_GUID_B]);
  });

  it('caps name length at 200 chars', () => {
    const long = 'x'.repeat(500);
    const s = parseUrlState(convertToParamMap({ name: long }), FIXED_NOW);
    expect(s.name.length).toBe(200);
  });
});

describe('buildUrlPatch', () => {
  it('emits null for default values so the URL stays clean', () => {
    const patch = buildUrlPatch({
      cityType: 'Mixed',
      targetYear: 2030,
      selectedIds: [],
      name: '',
    });
    expect(patch).toEqual({ city: null, year: null, t: null, name: null });
  });

  it('emits values for non-defaults', () => {
    const patch = buildUrlPatch({
      cityType: 'Industrial',
      targetYear: 2035,
      selectedIds: [VALID_GUID_A, VALID_GUID_B],
      name: 'My City',
    });
    expect(patch).toEqual({
      city: 'Industrial',
      year: '2035',
      t: `${VALID_GUID_A},${VALID_GUID_B}`,
      name: 'My City',
    });
  });
});

describe('parse / build round-trip', () => {
  it('survives a populated URL → state → URL', () => {
    const s1 = parseUrlState(
      convertToParamMap({
        city: 'Coastal',
        year: '2040',
        t: `${VALID_GUID_A}`,
        name: 'Test',
      }),
      FIXED_NOW,
    );
    const patch = buildUrlPatch(s1);
    const s2 = parseUrlState(
      convertToParamMap({
        city: patch['city'] ?? '',
        year: patch['year'] ?? '',
        t: patch['t'] ?? '',
        name: patch['name'] ?? '',
      }),
      FIXED_NOW,
    );
    expect(s2).toEqual(s1);
  });
});
