
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
import { TranslocoModule } from '@jsverse/transloco';
import type { Core, EventObject } from 'cytoscape';
import { mountCytoscape, buildPresetLayout } from '../lib/cytoscape-loader';
import { buildStylesheet } from '../lib/cytoscape-styles';
import { buildElements } from '../lib/elements';
import { computeRadialPositions } from '../lib/radial-layout';
import type { InteractiveMapNode } from '../knowledge-maps.types';

/**
 * Cytoscape canvas wrapper. Mounts the graph in `ngAfterViewInit`,
 * binds inputs reactively via `effect()`, and emits user-driven events.
 *
 * Hierarchy is encoded as parent-child edges derived from `parentId`.
 * Layout is computed by Cytoscape's breadthfirst algorithm — no server
 * coordinates. Re-running the layout after element rebuilds keeps the
 * tree readable even when the data changes.
 */
@Component({
  selector: 'cce-graph-canvas',
  standalone: true,
  imports: [MatIconModule, TranslocoModule],
  templateUrl: './graph-canvas.component.html',
  styleUrl: './graph-canvas.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class GraphCanvasComponent implements AfterViewInit, OnDestroy {
  readonly nodes = input.required<InteractiveMapNode[]>();
  readonly locale = input<'ar' | 'en'>('en');
  /** When locale changes to RTL the graph should refit — still tracked for this purpose. */
  readonly mirrored = input<boolean>(false);
  readonly selectedId = input<string | null>(null);
  readonly dimmedIds = input<ReadonlySet<string>>(new Set());

  readonly nodeClick = output<string>();
  readonly selectionChange = output<ReadonlySet<string>>();
  readonly clearFiltersRequest = output<void>();

  @ViewChild('host', { static: true }) hostRef!: ElementRef<HTMLDivElement>;

  readonly allDimmed = computed(() => {
    const count = this.nodes().length;
    if (count === 0) return false;
    return this.dimmedIds().size >= count;
  });

  private cy: Core | null = null;
  private shouldRefit = false;

  private readonly elements = computed(() =>
    buildElements(this.nodes(), { locale: this.locale() }),
  );

  /** Precomputed radial positions — depend only on the tree shape, not locale. */
  private readonly positions = computed(() => computeRadialPositions(this.nodes()));

  constructor() {
    // ─── Effect 1: rebuild elements + re-run layout when inputs change ───
    effect(() => {
      const els = this.elements();
      const cy = this.cy;
      if (!cy) return;
      const zoom = cy.zoom();
      const pan = cy.pan();
      cy.elements().remove();
      cy.add(els);
      const layout = cy.layout(buildPresetLayout(untracked(() => this.positions())));
      layout.run();
      if (this.shouldRefit) {
        cy.fit(undefined, 60);
        this.shouldRefit = false;
      } else {
        cy.zoom(zoom);
        cy.pan(pan);
      }
    });

    // Refit when locale flips (mirrored signal tracks locale changes).
    effect(() => {
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

    // ─── Effect 3: apply dimmedIds ───
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
      elements: untracked(() => this.elements()),
      style: buildStylesheet(),
      boxSelectionEnabled: true,
      positions: untracked(() => this.positions()),
    });

    this.cy.on('tap', 'node', (e: EventObject) => {
      this.nodeClick.emit(e.target.id());
    });
    this.cy.on('select unselect', 'node', () => {
      const cy = this.cy;
      if (!cy) return;
      const ids = new Set(cy.nodes(':selected').map((n) => n.id()));
      this.selectionChange.emit(ids);
    });

    this.cy.on('mouseover', 'node', (e: EventObject) => {
      e.target.addClass('cce-hover');
      this.hostRef.nativeElement.style.cursor = 'pointer';
    });
    this.cy.on('mouseout', 'node', (e: EventObject) => {
      e.target.removeClass('cce-hover');
      this.hostRef.nativeElement.style.cursor = '';
    });

    this.applySelectedId(untracked(() => this.selectedId()));
    this.applyDimmedIds(untracked(() => this.dimmedIds()));

    this.cy.fit(undefined, 40);
  }

  ngOnDestroy(): void {
    this.cy?.destroy();
    this.cy = null;
  }

  getCytoscape(): Core | null {
    return this.cy;
  }

  zoomIn(): void {
    const cy = this.cy;
    if (!cy) return;
    cy.animate({ zoom: cy.zoom() * 1.25, duration: 180 });
  }

  zoomOut(): void {
    const cy = this.cy;
    if (!cy) return;
    cy.animate({ zoom: cy.zoom() / 1.25, duration: 180 });
  }

  fit(): void {
    const cy = this.cy;
    if (!cy) return;
    cy.animate({ fit: { eles: cy.elements(), padding: 40 }, duration: 220 });
  }

  resetView(): void {
    const cy = this.cy;
    if (!cy) return;
    cy.elements().unselect();
    cy.animate({ fit: { eles: cy.elements(), padding: 40 }, duration: 220 });
  }

  clearFilters(): void {
    this.clearFiltersRequest.emit();
  }

  private applySelectedId(id: string | null): void {
    if (!this.cy) return;
    this.cy.elements().unselect();
    if (id) {
      const node = this.cy.$(`#${cssEscape(id)}`);
      if (node.length > 0) node.select();
    }
  }

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

function cssEscape(id: string): string {
  return id.replace(/([^a-zA-Z0-9_-])/g, '\\$1');
}
