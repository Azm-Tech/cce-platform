import {
  buildConfigurationJson,
  parseConfigurationJson,
  targetYearBounds,
  DEFAULT_CITY_TYPE,
  DEFAULT_TARGET_YEAR,
} from './interactive-city.types';

describe('interactive-city types helpers', () => {
  it('buildConfigurationJson serializes ids to a stable shape', () => {
    expect(buildConfigurationJson(['a', 'b'])).toBe('{"technologyIds":["a","b"]}');
    expect(buildConfigurationJson(new Set(['x']))).toBe('{"technologyIds":["x"]}');
    expect(buildConfigurationJson([])).toBe('{"technologyIds":[]}');
  });

  it('parseConfigurationJson is the inverse of buildConfigurationJson', () => {
    expect(parseConfigurationJson(buildConfigurationJson(['a', 'b']))).toEqual(['a', 'b']);
    expect(parseConfigurationJson('{"technologyIds":["x"]}')).toEqual(['x']);
  });

  it('parseConfigurationJson returns [] on malformed input', () => {
    expect(parseConfigurationJson('not json')).toEqual([]);
    expect(parseConfigurationJson('{}')).toEqual([]);
    expect(parseConfigurationJson('{"technologyIds":"oops"}')).toEqual([]);
    expect(parseConfigurationJson('{"technologyIds":[1,2,3]}')).toEqual([]);
  });

  it('targetYearBounds returns [year, year+50]', () => {
    const bounds = targetYearBounds(new Date('2030-06-01T00:00:00Z'));
    expect(bounds).toEqual({ min: 2030, max: 2080 });
  });

  it('exposes sensible defaults', () => {
    expect(DEFAULT_CITY_TYPE).toBe('Mixed');
    expect(DEFAULT_TARGET_YEAR).toBe(2030);
  });
});
