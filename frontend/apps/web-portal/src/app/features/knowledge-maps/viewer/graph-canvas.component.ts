import { CommonModule } from '@angular/common';
import {
  AfterViewInit,
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  OnDestroy,
  ViewChild,
  computed,
  effect,
  input,
  output,
  untracked,
} from '@angular/core';
import { MatIconModule } from '@angular/material/icon';
import { TranslateModule } from '@ngx-translate/core';
import type { Core, EventObject } from 'cytoscape';
import { mountCytoscape } from '../lib/cytoscape-loader';
import { buildStylesheet } from '../lib/cytoscape-styles';
import { buildElements } from '../lib/elements';
import type { KnowledgeMapEdge, KnowledgeMapNode } from '../knowledge-maps.types';

/**
 * Cytoscape canvas wrapper. Mounts the graph in `ngAfterViewInit`,
 * binds inputs reactively via `effect()`, and emits user-driven
 * events. The Cytoscape package itself is dynamically imported by
 * `cytoscape-loader` so this lazy-route stays a separate chunk.
 *
 * Inputs:
 *   nodes / edges    — typed DTOs from the active tab
 *   locale           — drives label selection (nameAr | nameEn)
 *   mirrored         — when true, x-coordinates are negated for RTL
 *   selectedId       — externally-controlled selection (e.g. URL ?node=)
 *   dimmedIds        — ids that should render at 0.3 opacity (search/filter)
 *
 * Outputs:
 *   nodeClick           — emits the clicked node id
 *   selectionChange     — emits the box-selection set as Cytoscape mutates it
 *   clearFiltersRequest — emitted from the all-dimmed empty state CTA
 *
 * Floating controls (zoom / fit / center / legend) sit on top of the
 * canvas as a brand-tinted glass overlay.
 */
@Component({
  selector: 'cce-graph-canvas',
  standalone: true,
  imports: [CommonModule, MatIconModule, TranslateModule],
  templateUrl: './graph-canvas.component.html',
  styleUrl: './graph-canvas.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class GraphCanvasComponent implements AfterViewInit, OnDestroy {
  readonly nodes = input.required<KnowledgeMapNode[]>();
  readonly edges = input.required<KnowledgeMapEdge[]>();
  readonly locale = input<'ar' | 'en'>('en');
  readonly mirrored = input<boolean>(false);
  readonly selectedId = input<string | null>(null);
  readonly dimmedIds = input<ReadonlySet<string>>(new Set());

  readonly nodeClick = output<string>();
  readonly selectionChange = output<ReadonlySet<string>>();
  readonly clearFiltersRequest = output<void>();

  @ViewChild('host', { static: true }) hostRef!: ElementRef<HTMLDivElement>;

  /** True when every node is currently dimmed by search/filter — drives
   *  the centered empty-state overlay. */
  readonly allDimmed = computed(() => {
    const count = this.nodes().length;
    if (count === 0) return false;
    return this.dimmedIds().size >= count;
  });

  private cy: Core | null = null;
  /** Set when the elements rebuild needs a fresh fit (initial mount, locale flip). */
  private shouldRefit = false;

  /** Computed elements — recomputes when nodes / edges / locale / mirrored change. */
  private readonly elements = computed(() =>
    buildElements(this.nodes(), this.edges(), {
      locale: this.locale(),
      mirrored: this.mirrored(),
    }),
  );

  constructor() {
    // ─── Effect 1: rebuild elements when inputs change (post-mount only) ───
    effect(() => {
      const els = this.elements();
      const cy = this.cy;
      if (!cy) return;
      // Preserve viewport state across rebuilds so users don't lose place
      // — but only after the user has interacted (post-initial-fit).
      const zoom = cy.zoom();
      const pan = cy.pan();
      cy.elements().remove();
      cy.add(els);
      // If this rebuild happened because the locale flipped (mirrored
      // changes x signs), the previous pan/zoom is no longer valid —
      // re-fit so the graph stays in view.
      if (this.shouldRefit) {
        cy.fit(undefined, 40);
        this.shouldRefit = false;
      } else {
        cy.zoom(zoom);
        cy.pan(pan);
      }
    });

    // Re-fit on locale flip — the elements rebuild swaps x signs and
    // would otherwise leave the user staring at empty canvas.
    effect(() => {
      // Subscribe to mirrored() so this effect fires on locale flips.
      this.mirrored();
      this.shouldRefit = true;
    });

    // ─── Effect 2: apply selectedId input ───
    effect(() => {
      const id = this.selectedId();
      const cy = this.cy;
      if (!cy) return;
      cy.elements().unselect();
      if (id) {
        const node = cy.$(`#${cssEscape(id)}`);
        if (node.length > 0) node.select();
      }
    });

    // ─── Effect 3: apply dimmedIds — both nodes and edges where either endpoint is dimmed ───
    effect(() => {
      const dimmed = this.dimmedIds();
      const cy = this.cy;
      if (!cy) return;
      cy.batch(() => {
        cy.nodes().forEach((n) => {
          if (dimmed.has(n.id())) n.addClass('cce-dim');
          else n.removeClass('cce-dim');
        });
        cy.edges().forEach((e) => {
          const src = e.source().id();
          const tgt = e.target().id();
          if (dimmed.has(src) || dimmed.has(tgt)) e.addClass('cce-dim');
          else e.removeClass('cce-dim');
        });
      });
    });
  }

  async ngAfterViewInit(): Promise<void> {
    this.cy = await mountCytoscape({
      container: this.hostRef.nativeElement,
      // Use untracked() so this initial read doesn't force the effects to run during mount.
      elements: untracked(() => this.elements()),
      style: buildStylesheet(),
      boxSelectionEnabled: true,
    });

    // ─── Wire Cytoscape events to component outputs ───
    this.cy.on('tap', 'node', (e: EventObject) => {
      this.nodeClick.emit(e.target.id());
    });
    this.cy.on('select unselect', 'node', () => {
      const cy = this.cy;
      if (!cy) return;
      const ids = new Set(cy.nodes(':selected').map((n) => n.id()));
      this.selectionChange.emit(ids);
    });

    // Hover halo — mouseover/out toggle the `cce-hover` class so the
    // stylesheet can paint a brand-green ring around the hovered node.
    this.cy.on('mouseover', 'node', (e: EventObject) => {
      e.target.addClass('cce-hover');
      this.hostRef.nativeElement.style.cursor = 'pointer';
    });
    this.cy.on('mouseout', 'node', (e: EventObject) => {
      e.target.removeClass('cce-hover');
      this.hostRef.nativeElement.style.cursor = '';
    });

    // Apply current input states post-mount.
    this.applySelectedId(untracked(() => this.selectedId()));
    this.applyDimmedIds(untracked(() => this.dimmedIds()));

    // Fit the viewport to the rendered graph with a small padding so users
    // don't have to manually zoom in. Without this, server-driven layouts
    // tend to render compressed in a corner of the canvas.
    this.cy.fit(undefined, 40);
  }

  ngOnDestroy(): void {
    this.cy?.destroy();
    this.cy = null;
  }

  /**
   * Returns the live Cytoscape Core, or null if the component has
   * not yet mounted (or has been destroyed). Used by the parent
   * MapViewerPage to feed cy into the export serializers.
   */
  getCytoscape(): Core | null {
    return this.cy;
  }

  // ─── Floating-controls handlers ───

  zoomIn(): void {
    const cy = this.cy;
    if (!cy) return;
    const next = cy.zoom() * 1.25;
    cy.animate({ zoom: next, duration: 180 });
  }

  zoomOut(): void {
    const cy = this.cy;
    if (!cy) return;
    const next = cy.zoom() / 1.25;
    cy.animate({ zoom: next, duration: 180 });
  }

  fit(): void {
    const cy = this.cy;
    if (!cy) return;
    cy.animate({ fit: { eles: cy.elements(), padding: 40 }, duration: 220 });
  }

  /** Reset the view: re-fit AND clear any selection. */
  resetView(): void {
    const cy = this.cy;
    if (!cy) return;
    cy.elements().unselect();
    cy.animate({ fit: { eles: cy.elements(), padding: 40 }, duration: 220 });
  }

  /** Emit a request to the parent to clear search + filter chips. */
  clearFilters(): void {
    this.clearFiltersRequest.emit();
  }

  /** Apply selectedId outside reactive context (used post-mount). */
  private applySelectedId(id: string | null): void {
    if (!this.cy) return;
    this.cy.elements().unselect();
    if (id) {
      const node = this.cy.$(`#${cssEscape(id)}`);
      if (node.length > 0) node.select();
    }
  }

  /** Apply dimmedIds outside reactive context (used post-mount). */
  private applyDimmedIds(ids: ReadonlySet<string>): void {
    if (!this.cy) return;
    const cy = this.cy;
    cy.batch(() => {
      cy.nodes().forEach((n) => {
        if (ids.has(n.id())) n.addClass('cce-dim');
        else n.removeClass('cce-dim');
      });
      cy.edges().forEach((e) => {
        const src = e.source().id();
        const tgt = e.target().id();
        if (ids.has(src) || ids.has(tgt)) e.addClass('cce-dim');
        else e.removeClass('cce-dim');
      });
    });
  }
}

/**
 * Escape a string for use as a CSS-ish id selector. Cytoscape selectors
 * follow CSS rules, so GUIDs with dashes need backslash-escaping. Safer
 * than relying on `CSS.escape` because that's not available in jsdom by
 * default.
 */
function cssEscape(id: string): string {
  return id.replace(/([^a-zA-Z0-9_-])/g, '\\$1');
}
