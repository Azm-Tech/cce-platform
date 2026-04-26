import { ExternalApi, InternalApi } from '../index';

describe('api-client generated namespaces', () => {
  it('exports ExternalApi namespace', () => {
    expect(ExternalApi).toBeDefined();
  });

  it('exports InternalApi namespace', () => {
    expect(InternalApi).toBeDefined();
  });

  it('ExternalApi namespace exposes at least one symbol from the generated SDK', () => {
    expect(Object.keys(ExternalApi).length).toBeGreaterThan(0);
  });

  it('InternalApi namespace exposes at least one symbol from the generated SDK', () => {
    expect(Object.keys(InternalApi).length).toBeGreaterThan(0);
  });
});
