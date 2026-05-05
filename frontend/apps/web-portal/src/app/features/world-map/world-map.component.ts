import { CommonModule } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  EventEmitter,
  HostListener,
  OnDestroy,
  OnInit,
  Output,
  ViewChild,
  inject,
  input,
} from '@angular/core';
import { HttpClient } from '@angular/common/http';
import * as d3 from 'd3';
import { feature } from 'topojson-client';
import { CITIES, type City } from './cities.data';

// We don't import @types/topojson-specification or @types/geojson here —
// the TopoJSON we ship is a fixed shape and we type just enough at the
// call sites to keep tsc happy without dragging in extra type packages.
type AnyTopology = { objects: Record<string, unknown> };
type GeoFeature = { type: 'Feature'; geometry: unknown; properties: unknown };
type GeoFeatureCollection = { type: 'FeatureCollection'; features: GeoFeature[] };

/**
 * Interactive world map. Renders countries (TopoJSON) + city markers
 * (D3 SVG circles) with hover, click, animated entry, and pan/zoom.
 *
 * Animations:
 *   - Countries fade in (600ms staggered by lat).
 *   - Markers drop-in with elastic ease (200ms staggered by population).
 *   - Markers pulse continuously (CSS).
 *   - Hovered country lifts via filter glow.
 *   - Selected marker shows expanding ring (CSS).
 */
@Component({
  selector: 'cce-world-map',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="cce-world-map" #host>
      <svg #svg class="cce-world-map__svg" role="img" aria-label="Interactive world map of cities">
        <defs>
          <radialGradient id="oceanGradient" cx="50%" cy="50%" r="70%">
            <stop offset="0%" stop-color="#0b1d3a" />
            <stop offset="100%" stop-color="#020617" />
          </radialGradient>
          <filter id="countryGlow" x="-20%" y="-20%" width="140%" height="140%">
            <feGaussianBlur stdDeviation="1.5" result="blur" />
            <feMerge>
              <feMergeNode in="blur" />
              <feMergeNode in="SourceGraphic" />
            </feMerge>
          </filter>
          <filter id="markerGlow" x="-50%" y="-50%" width="200%" height="200%">
            <feGaussianBlur stdDeviation="2" result="blur" />
            <feMerge>
              <feMergeNode in="blur" />
              <feMergeNode in="SourceGraphic" />
            </feMerge>
          </filter>
        </defs>
        <rect class="cce-world-map__ocean" width="100%" height="100%" fill="url(#oceanGradient)" />
        <g #zoomGroup class="cce-world-map__zoom-group">
          <g class="cce-world-map__graticule"></g>
          <g class="cce-world-map__countries"></g>
          <g class="cce-world-map__cities"></g>
        </g>
      </svg>
    </div>
  `,
  styles: [
    `
      :host { display: block; width: 100%; height: 100%; }
      .cce-world-map { position: relative; width: 100%; height: 100%; overflow: hidden; }
      .cce-world-map__svg { width: 100%; height: 100%; display: block; cursor: grab; }
      .cce-world-map__svg:active { cursor: grabbing; }

      /* Countries */
      :host ::ng-deep .cce-world-map__countries path {
        fill: #1e3a5f;
        stroke: #2d5a87;
        stroke-width: 0.4;
        opacity: 0;
        transition: fill 0.25s ease, transform 0.25s ease;
        cursor: default;
      }
      :host ::ng-deep .cce-world-map__countries path.cce-country--visible {
        animation: countryFadeIn 700ms ease-out forwards;
      }
      :host ::ng-deep .cce-world-map__countries path:hover {
        fill: #2d5a87;
        filter: url(#countryGlow);
      }

      /* Graticule (subtle grid) */
      :host ::ng-deep .cce-world-map__graticule path {
        fill: none;
        stroke: rgba(255, 255, 255, 0.04);
        stroke-width: 0.3;
        opacity: 0;
        animation: graticuleFadeIn 1.5s ease-out 0.3s forwards;
      }

      /* City markers */
      :host ::ng-deep .cce-world-map__cities .cce-city-marker {
        cursor: pointer;
        opacity: 0;
        transform-origin: center;
        animation: markerDropIn 800ms cubic-bezier(0.34, 1.56, 0.64, 1) forwards;
      }
      :host ::ng-deep .cce-world-map__cities .cce-city-marker__core {
        fill: #f4a300;
        stroke: #fff;
        stroke-width: 1.2;
        filter: url(#markerGlow);
        transition: fill 0.2s ease, r 0.2s ease;
      }
      :host ::ng-deep .cce-world-map__cities .cce-city-marker--low .cce-city-marker__core {
        fill: #4ade80;
      }
      :host ::ng-deep .cce-world-map__cities .cce-city-marker--medium .cce-city-marker__core {
        fill: #fbbf24;
      }
      :host ::ng-deep .cce-world-map__cities .cce-city-marker--high .cce-city-marker__core {
        fill: #f87171;
      }
      :host ::ng-deep .cce-world-map__cities .cce-city-marker__pulse {
        fill: none;
        stroke: currentColor;
        stroke-width: 1.5;
        transform-origin: center;
        animation: markerPulse 2.4s ease-out infinite;
      }
      :host ::ng-deep .cce-world-map__cities .cce-city-marker--low .cce-city-marker__pulse { color: #4ade80; }
      :host ::ng-deep .cce-world-map__cities .cce-city-marker--medium .cce-city-marker__pulse { color: #fbbf24; }
      :host ::ng-deep .cce-world-map__cities .cce-city-marker--high .cce-city-marker__pulse { color: #f87171; }
      :host ::ng-deep .cce-world-map__cities .cce-city-marker:hover .cce-city-marker__core {
        r: 7;
      }
      :host ::ng-deep .cce-world-map__cities .cce-city-marker--selected .cce-city-marker__core {
        stroke: #fff;
        stroke-width: 2.5;
        r: 8;
      }
      :host ::ng-deep .cce-world-map__cities .cce-city-marker--selected .cce-city-marker__ring {
        fill: none;
        stroke: #fff;
        stroke-width: 2;
        animation: selectedRing 1.8s ease-out infinite;
        transform-origin: center;
      }

      /* Tooltip label */
      :host ::ng-deep .cce-world-map__cities .cce-city-marker__label {
        pointer-events: none;
        fill: #fff;
        font-family: ui-sans-serif, system-ui, sans-serif;
        font-size: 11px;
        font-weight: 600;
        text-shadow: 0 1px 4px rgba(0, 0, 0, 0.8);
        opacity: 0;
        transition: opacity 0.15s ease;
      }
      :host ::ng-deep .cce-world-map__cities .cce-city-marker:hover .cce-city-marker__label,
      :host ::ng-deep .cce-world-map__cities .cce-city-marker--selected .cce-city-marker__label {
        opacity: 1;
      }

      @keyframes countryFadeIn {
        from { opacity: 0; transform: translateY(6px); }
        to   { opacity: 0.85; transform: translateY(0); }
      }
      @keyframes graticuleFadeIn {
        from { opacity: 0; }
        to   { opacity: 1; }
      }
      @keyframes markerDropIn {
        0%   { opacity: 0; transform: translateY(-30px) scale(0.3); }
        70%  { opacity: 1; transform: translateY(2px) scale(1.2); }
        100% { opacity: 1; transform: translateY(0) scale(1); }
      }
      @keyframes markerPulse {
        0%   { r: 5; opacity: 0.8; }
        70%  { r: 22; opacity: 0; }
        100% { r: 22; opacity: 0; }
      }
      @keyframes selectedRing {
        0%   { r: 8; opacity: 0.8; }
        100% { r: 36; opacity: 0; }
      }
    `,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WorldMapComponent implements OnInit, OnDestroy {
  @ViewChild('host', { static: true }) hostEl!: ElementRef<HTMLDivElement>;
  @ViewChild('svg', { static: true }) svgEl!: ElementRef<SVGSVGElement>;
  @ViewChild('zoomGroup', { static: true }) zoomGroupEl!: ElementRef<SVGGElement>;

  readonly selectedCityId = input<string | null>(null);
  @Output() readonly cityClicked = new EventEmitter<City>();

  private readonly http = inject(HttpClient);
  private resizeObserver?: ResizeObserver;
  private projection!: d3.GeoProjection;
  private path!: d3.GeoPath;
  private zoom!: d3.ZoomBehavior<SVGSVGElement, unknown>;
  private currentSelectedId: string | null = null;

  async ngOnInit(): Promise<void> {
    const topology = await new Promise<AnyTopology>((resolve) => {
      this.http
        .get<AnyTopology>('/assets/world-110m.json')
        .subscribe((data) => resolve(data));
    });
    this.render(topology);
    this.observeResize();
  }

  ngOnDestroy(): void {
    this.resizeObserver?.disconnect();
  }

  ngOnChanges(): void {
    this.applySelection();
  }

  @HostListener('window:resize')
  onWindowResize(): void {
    this.handleResize();
  }

  private observeResize(): void {
    this.resizeObserver = new ResizeObserver(() => this.handleResize());
    this.resizeObserver.observe(this.hostEl.nativeElement);
  }

  private handleResize(): void {
    const { width, height } = this.hostEl.nativeElement.getBoundingClientRect();
    if (width === 0 || height === 0) return;
    this.projection.fitSize([width, height], { type: 'Sphere' } as never);
    this.path = d3.geoPath(this.projection);
    const svg = d3.select(this.svgEl.nativeElement);
    const pathFn = (d: unknown) => this.path(d as never) ?? '';
    svg.selectAll<SVGPathElement, unknown>('.cce-world-map__countries path').attr('d', pathFn);
    svg.selectAll<SVGPathElement, unknown>('.cce-world-map__graticule path').attr('d', pathFn);
    this.repositionMarkers();
  }

  private render(topology: AnyTopology): void {
    const { width, height } = this.hostEl.nativeElement.getBoundingClientRect();
    const w = width || 1200;
    const h = height || 600;

    this.projection = d3
      .geoNaturalEarth1()
      .fitSize([w, h], { type: 'Sphere' } as never);
    this.path = d3.geoPath(this.projection);

    const svg = d3.select(this.svgEl.nativeElement);
    svg.attr('viewBox', `0 0 ${w} ${h}`).attr('preserveAspectRatio', 'xMidYMid meet');

    const pathFn = (d: unknown) => this.path(d as never) ?? '';

    // Graticule: light grid lines.
    const graticule = d3.geoGraticule().step([20, 20]);
    svg
      .select<SVGGElement>('.cce-world-map__graticule')
      .selectAll('path')
      .data([graticule()])
      .enter()
      .append('path')
      .attr('d', pathFn);

    // Countries.
    const countries = (
      feature(topology as never, topology.objects['countries'] as never) as unknown as GeoFeatureCollection
    ).features;
    const sorted = countries.slice().sort((a, b) => {
      // Stagger by latitude (north-to-south fade-in).
      const ay = (d3.geoCentroid(a as never)[1] ?? 0) * -1;
      const by = (d3.geoCentroid(b as never)[1] ?? 0) * -1;
      return ay - by;
    });
    svg
      .select<SVGGElement>('.cce-world-map__countries')
      .selectAll('path')
      .data(sorted)
      .enter()
      .append('path')
      .attr('d', pathFn)
      .attr('class', 'cce-country')
      .style('animation-delay', (_d, i) => `${i * 6}ms`)
      // Defer adding the visibility class so the animation actually runs.
      .each(function () {
        const node = this as SVGPathElement;
        requestAnimationFrame(() => node.classList.add('cce-country--visible'));
      });

    // City markers — animation order by descending population (large first).
    const sortedCities = CITIES.slice().sort((a, b) => b.population - a.population);
    const cityG = svg.select<SVGGElement>('.cce-world-map__cities');
    const markers = cityG
      .selectAll('g.cce-city-marker')
      .data(sortedCities, (d) => (d as City).id)
      .enter()
      .append('g')
      .attr('class', (d) => `cce-city-marker cce-city-marker--${d.carbonTier}`)
      .attr('data-city-id', (d) => d.id)
      .attr('transform', (d) => {
        const p = this.projection([d.lon, d.lat]);
        return p ? `translate(${p[0]}, ${p[1]})` : null;
      })
      .style('animation-delay', (_d, i) => `${i * 25 + 600}ms`)
      .on('click', (event: MouseEvent, d) => {
        event.stopPropagation();
        this.cityClicked.emit(d);
      });

    markers.append('circle').attr('class', 'cce-city-marker__pulse').attr('r', 5);
    markers.append('circle').attr('class', 'cce-city-marker__ring').attr('r', 8);
    markers.append('circle').attr('class', 'cce-city-marker__core').attr('r', 5);
    markers
      .append('text')
      .attr('class', 'cce-city-marker__label')
      .attr('y', -10)
      .attr('text-anchor', 'middle')
      .text((d) => d.name);

    // Pan + zoom.
    this.zoom = d3
      .zoom<SVGSVGElement, unknown>()
      .scaleExtent([1, 8])
      .on('zoom', (event) => {
        d3.select(this.zoomGroupEl.nativeElement).attr('transform', event.transform.toString());
        // Counter-scale marker visuals so they stay readable when zoomed in.
        const k = event.transform.k;
        d3.select(this.zoomGroupEl.nativeElement)
          .selectAll<SVGGElement, City>('g.cce-city-marker')
          .each(function () {
            const node = this as SVGGElement;
            const t = node.getAttribute('transform') ?? '';
            // Strip any prior scale we might have appended.
            const baseTranslate = t.replace(/\s*scale\([^)]*\)/, '');
            node.setAttribute('transform', `${baseTranslate} scale(${1 / k})`);
          });
      });
    svg.call(this.zoom);

    this.applySelection();
  }

  private repositionMarkers(): void {
    const cityG = d3.select(this.svgEl.nativeElement).select<SVGGElement>('.cce-world-map__cities');
    cityG.selectAll<SVGGElement, City>('g.cce-city-marker').attr('transform', (d) => {
      const p = this.projection([d.lon, d.lat]);
      return p ? `translate(${p[0]}, ${p[1]})` : null;
    });
  }

  private applySelection(): void {
    const id = this.selectedCityId();
    if (id === this.currentSelectedId) return;
    this.currentSelectedId = id;
    d3.select(this.svgEl.nativeElement)
      .select<SVGGElement>('.cce-world-map__cities')
      .selectAll<SVGGElement, City>('g.cce-city-marker')
      .classed('cce-city-marker--selected', (d) => d.id === id);
  }
}
