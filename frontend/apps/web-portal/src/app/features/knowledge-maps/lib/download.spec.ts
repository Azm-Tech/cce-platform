import { buildFilename, downloadBlob } from './download';

describe('downloadBlob', () => {
  let createObjectURLSpy: jest.SpyInstance;
  let revokeObjectURLSpy: jest.SpyInstance;
  let appendChildSpy: jest.SpyInstance;
  let removeChildSpy: jest.SpyInstance;

  beforeEach(() => {
    jest.useFakeTimers();
    // jsdom doesn't ship URL.createObjectURL — polyfill before spying.
    if (typeof URL.createObjectURL !== 'function') {
      (URL as unknown as { createObjectURL: () => string }).createObjectURL = () => 'blob:fake-url';
    }
    if (typeof URL.revokeObjectURL !== 'function') {
      (URL as unknown as { revokeObjectURL: (u: string) => void }).revokeObjectURL = () => undefined;
    }
    createObjectURLSpy = jest
      .spyOn(URL, 'createObjectURL')
      .mockReturnValue('blob:fake-url');
    revokeObjectURLSpy = jest
      .spyOn(URL, 'revokeObjectURL')
      .mockImplementation(() => undefined);
    appendChildSpy = jest.spyOn(document.body, 'appendChild');
    removeChildSpy = jest.spyOn(document.body, 'removeChild');
  });

  afterEach(() => {
    jest.useRealTimers();
    jest.restoreAllMocks();
  });

  it('creates an object URL, triggers a click, removes the anchor, and revokes the URL on the next tick', () => {
    const blob = new Blob(['hello'], { type: 'text/plain' });
    const clickSpy = jest.spyOn(HTMLAnchorElement.prototype, 'click').mockImplementation(() => undefined);

    downloadBlob(blob, 'demo.txt');

    expect(createObjectURLSpy).toHaveBeenCalledWith(blob);
    expect(appendChildSpy).toHaveBeenCalled();
    expect(clickSpy).toHaveBeenCalledTimes(1);
    expect(removeChildSpy).toHaveBeenCalled();
    expect(revokeObjectURLSpy).not.toHaveBeenCalled();
    jest.runAllTimers();
    expect(revokeObjectURLSpy).toHaveBeenCalledWith('blob:fake-url');
  });
});

describe('buildFilename', () => {
  it('returns the expected pattern with today\'s date', () => {
    const out = buildFilename('circular-economy', 'png');
    // Expecting "knowledge-map-circular-economy-YYYY-MM-DD.png"
    expect(out).toMatch(/^knowledge-map-circular-economy-\d{4}-\d{2}-\d{2}\.png$/);
  });

  it('sanitizes slugs with special characters and uppercase', () => {
    const out = buildFilename('Circular!Economy / Test_2', 'json');
    // "!", "/", "_", and spaces all collapse to hyphens; lowercased.
    expect(out).toMatch(/^knowledge-map-circular-economy-test-2-\d{4}-\d{2}-\d{2}\.json$/);
  });

  it('falls back to "map" when slug sanitization leaves nothing', () => {
    const out = buildFilename('!!!', 'svg');
    expect(out).toMatch(/^knowledge-map-map-\d{4}-\d{2}-\d{2}\.svg$/);
  });
});
