/**
 * Deterministic radial tree layout for the knowledge-map graph.
 *
 * Cytoscape's built-in `concentric` layout places nodes on rings purely by a
 * numeric value and ignores the parent→child grouping, so sibling branches
 * overlap and labels collide. This computes explicit positions instead:
 *
 *   - the root sits at the centre (0, 0);
 *   - every branch is given its own angular WEDGE, sized by how many leaves it
 *     contains (busy branches get more room → no crowding);
 *   - a node's children fan out WITHIN their parent's wedge, one ring further
 *     out, so each child stays visually next to its parent.
 *
 * The result is fed to Cytoscape's `preset` layout. Pure + deterministic
 * (no Date/Math.random) so it is resume- and test-safe.
 */

export interface RadialNode {
  id: string;
  level: number;
  parentId?: string | null;
}

export interface RadialLayoutOptions {
  /** Distance between successive rings, in px. Larger ⇒ more spread. */
  ringGap?: number;
  /** Rotation of the whole map, in radians. Default points the first branch up. */
  startAngle?: number;
}

export type PositionMap = Record<string, { x: number; y: number }>;

const DEFAULT_RING_GAP = 240;
const DEFAULT_START_ANGLE = -Math.PI / 2; // first branch points "up"

export function computeRadialPositions(
  nodes: readonly RadialNode[],
  opts: RadialLayoutOptions = {},
): PositionMap {
  const ringGap = opts.ringGap ?? DEFAULT_RING_GAP;
  const startAngle = opts.startAngle ?? DEFAULT_START_ANGLE;

  const byId = new Map(nodes.map((n) => [n.id, n]));
  const children = new Map<string, string[]>();
  const roots: string[] = [];

  for (const n of nodes) {
    const parent = n.parentId && byId.has(n.parentId) ? n.parentId : null;
    if (parent) {
      const bucket = children.get(parent) ?? [];
      bucket.push(n.id);
      children.set(parent, bucket);
    } else {
      roots.push(n.id);
    }
  }

  // Weight = number of leaves under a node — used to size each branch's wedge.
  const weight = new Map<string, number>();
  const calcWeight = (id: string): number => {
    const kids = children.get(id) ?? [];
    if (kids.length === 0) {
      weight.set(id, 1);
      return 1;
    }
    let sum = 0;
    for (const k of kids) sum += calcWeight(k);
    weight.set(id, sum);
    return sum;
  };
  roots.forEach(calcWeight);

  const pos: PositionMap = {};

  const place = (id: string, depth: number, a0: number, a1: number): void => {
    const mid = (a0 + a1) / 2;
    const r = depth * ringGap;
    pos[id] = depth === 0 ? { x: 0, y: 0 } : { x: r * Math.cos(mid), y: r * Math.sin(mid) };

    const kids = children.get(id) ?? [];
    if (kids.length === 0) return;
    const total = kids.reduce((s, k) => s + (weight.get(k) ?? 1), 0) || 1;
    let a = a0;
    for (const k of kids) {
      const span = (a1 - a0) * ((weight.get(k) ?? 1) / total);
      place(k, depth + 1, a, a + span);
      a += span;
    }
  };

  if (roots.length === 1) {
    // Single centre node — children fan around the full circle.
    place(roots[0], 0, startAngle, startAngle + 2 * Math.PI);
  } else if (roots.length > 1) {
    // Multiple roots — give each its own wedge on the first ring (no centre).
    const total = roots.reduce((s, r) => s + (weight.get(r) ?? 1), 0) || 1;
    let a = startAngle;
    for (const r of roots) {
      const span = 2 * Math.PI * ((weight.get(r) ?? 1) / total);
      place(r, 1, a, a + span);
      a += span;
    }
  }

  return pos;
}
