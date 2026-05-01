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
 *   nodeClick        — emits the clicked node id
 *   selectionChange  — emits the box-selection set as Cytoscape mutates it
 */
@Component({
  selector: 'cce-graph-canvas',
  standalone: true,
  imports: [CommonModule],
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

  @ViewChild('host', { static: true }) hostRef!: ElementRef<HTMLDivElement>;

  private cy: Core | null = null;

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
      // Preserve viewport state across rebuilds so users don't lose place.
      const zoom = cy.zoom();
      const pan = cy.pan();
      cy.elements().remove();
      cy.add(els);
      cy.zoom(zoom);
      cy.pan(pan);
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

    // Apply current input states post-mount.
    this.applySelectedId(untracked(() => this.selectedId()));
    this.applyDimmedIds(untracked(() => this.dimmedIds()));
  }

  ngOnDestroy(): void {
    this.cy?.destroy();
    this.cy = null;
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
