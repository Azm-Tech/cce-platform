import { _resetForTest, ensureSvgPlugin, loadCytoscape } from './cytoscape-loader';

// Real modules now exist (added in Phase 0.1), so we mock them via
// jest.mock without virtual:true. The mock factory still produces a
// jest-spy `default.use()` for the spec to assert against.
jest.mock('cytoscape', () => ({
  __esModule: true,
  default: Object.assign(jest.fn(), { use: jest.fn() }),
}));
jest.mock('cytoscape-svg', () => ({
  __esModule: true,
  default: { name: 'svg-plugin' },
}));

describe('cytoscape-loader', () => {
  beforeEach(() => _resetForTest());

  it('loadCytoscape memoizes the import promise across calls', async () => {
    const a = await loadCytoscape();
    const b = await loadCytoscape();
    expect(a).toBe(b);
  });

  it('ensureSvgPlugin registers the plugin exactly once across N calls', async () => {
    const cy = await loadCytoscape();
    const useSpy = (cy as unknown as { use: jest.Mock }).use;
    useSpy.mockClear();
    await ensureSvgPlugin();
    await ensureSvgPlugin();
    await ensureSvgPlugin();
    expect(useSpy).toHaveBeenCalledTimes(1);
  });

  it('_resetForTest clears the singleton state for isolation', async () => {
    await loadCytoscape();
    _resetForTest();
    const cy = await loadCytoscape();
    expect(cy).toBeDefined();
    expect(typeof cy).toBe('function');
  });
});
