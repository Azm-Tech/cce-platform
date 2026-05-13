import { CommonModule } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  effect,
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
import { CITIES_EXTRA } from './cities-extra.data';

/**
 * Combined city dataset. Featured cities (60) carry full metadata
 * (initiatives, summary). Standard cities (~150) carry only basics.
 * The detail panel handles both shapes.
 */
export type FeaturedCity = City & { kind: 'featured' };
export type StandardCity = (typeof CITIES_EXTRA)[number];
export type AnyCity = FeaturedCity | StandardCity;

export const ALL_CITIES: readonly AnyCity[] = [
  ...CITIES.map((c): FeaturedCity => ({ ...c, kind: 'featured' as const })),
  ...CITIES_EXTRA,
];

/** Country-name lookup table (keys: numeric ISO 3166-1 IDs from world-atlas). */
const COUNTRY_NAMES: Record<string, string> = {
  '4':  'Afghanistan',  '8':  'Albania',     '12': 'Algeria',     '20': 'Andorra',     '24': 'Angola',
  '32': 'Argentina',    '51': 'Armenia',     '36': 'Australia',   '40': 'Austria',     '31': 'Azerbaijan',
  '44': 'Bahamas',      '48': 'Bahrain',     '50': 'Bangladesh',  '112':'Belarus',     '56': 'Belgium',
  '84': 'Belize',       '204':'Benin',       '64': 'Bhutan',      '68': 'Bolivia',     '70': 'Bosnia',
  '72': 'Botswana',     '76': 'Brazil',      '96': 'Brunei',      '100':'Bulgaria',    '854':'Burkina Faso',
  '108':'Burundi',      '116':'Cambodia',    '120':'Cameroon',    '124':'Canada',      '140':'CAR',
  '148':'Chad',         '152':'Chile',       '156':'China',       '170':'Colombia',    '178':'Congo',
  '180':'DR Congo',     '188':'Costa Rica',  '384':'Côte d’Ivoire','191':'Croatia',    '192':'Cuba',
  '196':'Cyprus',       '203':'Czechia',     '208':'Denmark',     '262':'Djibouti',    '214':'Dominican Rep.',
  '218':'Ecuador',      '818':'Egypt',       '222':'El Salvador', '226':'Eq. Guinea',  '232':'Eritrea',
  '233':'Estonia',      '231':'Ethiopia',    '242':'Fiji',        '246':'Finland',     '250':'France',
  '266':'Gabon',        '270':'Gambia',      '268':'Georgia',     '276':'Germany',     '288':'Ghana',
  '300':'Greece',       '320':'Guatemala',   '324':'Guinea',      '624':'Guinea-Bissau','328':'Guyana',
  '332':'Haiti',        '340':'Honduras',    '348':'Hungary',     '352':'Iceland',     '356':'India',
  '360':'Indonesia',    '364':'Iran',        '368':'Iraq',        '372':'Ireland',     '376':'Israel',
  '380':'Italy',        '388':'Jamaica',     '392':'Japan',       '400':'Jordan',      '398':'Kazakhstan',
  '404':'Kenya',        '410':'South Korea', '408':'North Korea', '414':'Kuwait',      '417':'Kyrgyzstan',
  '418':'Laos',         '428':'Latvia',      '422':'Lebanon',     '426':'Lesotho',     '430':'Liberia',
  '434':'Libya',        '440':'Lithuania',   '442':'Luxembourg',  '450':'Madagascar',  '454':'Malawi',
  '458':'Malaysia',     '466':'Mali',        '478':'Mauritania',  '484':'Mexico',      '498':'Moldova',
  '496':'Mongolia',     '499':'Montenegro',  '504':'Morocco',     '508':'Mozambique',  '104':'Myanmar',
  '516':'Namibia',      '524':'Nepal',       '528':'Netherlands', '554':'New Zealand', '558':'Nicaragua',
  '562':'Niger',        '566':'Nigeria',     '578':'Norway',      '512':'Oman',        '586':'Pakistan',
  '275':'Palestine',    '591':'Panama',      '598':'PNG',         '600':'Paraguay',    '604':'Peru',
  '608':'Philippines',  '616':'Poland',      '620':'Portugal',    '630':'Puerto Rico', '634':'Qatar',
  '642':'Romania',      '643':'Russia',      '646':'Rwanda',      '682':'Saudi Arabia','686':'Senegal',
  '688':'Serbia',       '694':'Sierra Leone','702':'Singapore',   '703':'Slovakia',    '705':'Slovenia',
  '706':'Somalia',      '710':'South Africa','728':'South Sudan', '724':'Spain',       '144':'Sri Lanka',
  '729':'Sudan',        '740':'Suriname',    '748':'Eswatini',    '752':'Sweden',      '756':'Switzerland',
  '760':'Syria',        '158':'Taiwan',      '762':'Tajikistan',  '834':'Tanzania',    '764':'Thailand',
  '626':'Timor-Leste',  '768':'Togo',        '780':'Trinidad',    '788':'Tunisia',     '792':'Türkiye',
  '795':'Turkmenistan', '800':'Uganda',      '804':'Ukraine',     '784':'UAE',         '826':'United Kingdom',
  '840':'United States','858':'Uruguay',     '860':'Uzbekistan',  '548':'Vanuatu',     '862':'Venezuela',
  '704':'Vietnam',      '887':'Yemen',       '894':'Zambia',      '716':'Zimbabwe',
};

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
          <!-- ClipPath populated dynamically with country paths so the
               Voronoi city-boundary tessellation only renders on land. -->
          <clipPath id="cce-land-clip"></clipPath>
        </defs>
        <rect class="cce-world-map__ocean" width="100%" height="100%" fill="url(#oceanGradient)" />
        <g #zoomGroup class="cce-world-map__zoom-group">
          <g class="cce-world-map__graticule"></g>
          <g class="cce-world-map__countries"></g>
          <g class="cce-world-map__city-boundaries" clip-path="url(#cce-land-clip)"></g>
          <g class="cce-world-map__country-labels"></g>
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

      /* Countries — high-contrast borders + clear fill */
      :host ::ng-deep .cce-world-map__countries path {
        fill: #1e3a5f;
        stroke: rgba(255, 255, 255, 0.7);
        stroke-width: 1.4;
        stroke-linejoin: round;
        vector-effect: non-scaling-stroke;
        opacity: 0;
        transition: fill 0.25s ease;
        cursor: default;
      }
      :host ::ng-deep .cce-world-map__countries path:hover {
        fill: #2d5a87;
        stroke: #ffffff;
      }
      :host ::ng-deep .cce-world-map__countries path.cce-country--visible {
        animation: countryFadeIn 700ms ease-out forwards;
      }

      /* Graticule (subtle grid) */
      :host ::ng-deep .cce-world-map__graticule path {
        fill: none;
        stroke: rgba(255, 255, 255, 0.04);
        stroke-width: 0.3;
        opacity: 0;
        animation: graticuleFadeIn 1.5s ease-out 0.3s forwards;
      }

      /* Country labels — visible at all zoom levels with size + opacity scaling. */
      :host ::ng-deep .cce-world-map__country-labels text {
        fill: rgba(255, 255, 255, 0.55);
        font-family: ui-sans-serif, system-ui, sans-serif;
        font-weight: 600;
        font-size: 9px;
        text-anchor: middle;
        text-shadow: 0 1px 3px rgba(0, 0, 0, 0.85);
        pointer-events: none;
        opacity: 0;
        transition: opacity 0.3s ease, fill 0.2s ease;
        animation: countryLabelFadeIn 800ms ease-out 1.4s forwards;
      }
      /* Bigger labels for major countries (rendered with cce-country-label--major class). */
      :host ::ng-deep .cce-world-map__country-labels text.cce-country-label--major {
        font-size: 11px;
        fill: rgba(255, 255, 255, 0.75);
      }
      /* Hidden until zoom reveals them (small countries). */
      :host ::ng-deep .cce-world-map__country-labels text.cce-country-label--hidden {
        opacity: 0;
        pointer-events: none;
      }
      :host ::ng-deep .cce-world-map__country-labels.cce-labels--zoomed-deep text.cce-country-label--hidden {
        opacity: 0.7;
      }

      /* Standard cities — always visible, smaller, no pulse. The
         featured cities (60) animate in with bounce + pulse; standard
         cities (~150) appear with a simpler quick fade. */
      :host ::ng-deep .cce-world-map__cities .cce-city-marker--standard {
        animation: standardMarkerFadeIn 500ms ease-out forwards !important;
        opacity: 0;
        pointer-events: auto;
      }
      :host ::ng-deep .cce-world-map__cities .cce-city-marker--standard .cce-city-marker__core {
        stroke-width: 0.8;
      }
      :host ::ng-deep .cce-world-map__cities .cce-city-marker--standard .cce-city-marker__pulse {
        animation: none;
        opacity: 0;
      }
      /* When zoomed in, standard markers brighten to full opacity. */
      :host ::ng-deep .cce-world-map__cities.cce-cities--zoomed-in .cce-city-marker--standard {
        opacity: 1;
      }
      @keyframes standardMarkerFadeIn {
        from { opacity: 0; }
        to   { opacity: 1; }
      }

      /* Filtered-out: marker hidden via class set from page filter signals. */
      :host ::ng-deep .cce-world-map__cities .cce-city-marker--filtered-out {
        opacity: 0 !important;
        pointer-events: none !important;
        transition: opacity 0.25s ease;
      }

      /* Per-city geo-circle boundaries — each city has its own discrete
         circular footprint sized by population on a log scale. Discs are
         great-circle on the sphere then projected, so they distort with
         the map projection. Clipped to land via #cce-land-clip. */
      :host ::ng-deep .cce-world-map__city-boundaries path {
        fill-opacity: 0.14;
        stroke: rgba(255, 255, 255, 0.30);
        stroke-width: 0.8;
        stroke-linejoin: round;
        vector-effect: non-scaling-stroke;
        pointer-events: none; /* clicks fall through to city markers */
        opacity: 0;
        transition: fill-opacity 0.2s ease, stroke-width 0.2s ease, stroke 0.2s ease;
        animation: cityBoundaryFadeIn 900ms ease-out 1.4s forwards;
      }
      :host ::ng-deep .cce-world-map__city-boundaries path.cce-city-boundary--low    { fill: #4ade80; }
      :host ::ng-deep .cce-world-map__city-boundaries path.cce-city-boundary--medium { fill: #fbbf24; }
      :host ::ng-deep .cce-world-map__city-boundaries path.cce-city-boundary--high   { fill: #f87171; }
      :host ::ng-deep .cce-world-map__city-boundaries path.cce-city-boundary--filtered-out {
        opacity: 0 !important;
      }
      /* Hover state — driven via JS class toggle when the matching
         city marker is hovered. Pulls the boundary forward visually. */
      :host ::ng-deep .cce-world-map__city-boundaries path.cce-city-boundary--hover {
        fill-opacity: 0.30;
        stroke: rgba(255, 255, 255, 0.65);
        stroke-width: 1.4;
      }
      /* Selection state — strongest highlight, applied when a city is
         actively selected from the list / detail panel. */
      :host ::ng-deep .cce-world-map__city-boundaries path.cce-city-boundary--selected {
        fill-opacity: 0.42;
        stroke: rgba(255, 255, 255, 0.85);
        stroke-width: 1.8;
      }
      @keyframes cityBoundaryFadeIn {
        from { opacity: 0; }
        to   { opacity: 1; }
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
      @keyframes countryLabelFadeIn {
        from { opacity: 0; }
        to   { opacity: 1; }
      }
      @keyframes graticuleFadeIn {
        from { opacity: 0; }
        to   { opacity: 1; }
      }
      /* Markers fade in (opacity only — must NOT animate transform,
         else the CSS keyframe transform overrides the SVG transform
         attribute that holds the projected lat/lon coords, causing
         all markers to collapse to translate(0,0). The bounce-in
         visual is sacrificed so positions stay correct under zoom. */
      @keyframes markerDropIn {
        from { opacity: 0; }
        to   { opacity: 1; }
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
  /** IDs of cities that should currently be visible (post-filter). null = no filter active. */
  readonly visibleCityIds = input<readonly string[] | null>(null);
  @Output() readonly cityClicked = new EventEmitter<AnyCity>();
  /** Fires when the user clicks empty map area (ocean / non-pin space)
   *  — the parent uses this to close the detail panel. */
  @Output() readonly mapBackgroundClicked = new EventEmitter<void>();

  private readonly http = inject(HttpClient);
  private resizeObserver?: ResizeObserver;
  private projection!: d3.GeoProjection;
  private path!: d3.GeoPath;
  private zoom!: d3.ZoomBehavior<SVGSVGElement, unknown>;
  private currentSelectedId: string | null = null;
  /** True once render() has wired up the zoom behavior. Prevents the
   *  fit-to-filter effect from running before the map is ready. */
  private isMapReady = false;

  constructor() {
    // When the filter changes (page emits a new visibleCityIds list),
    // smoothly animate the map to fit those cities. Clearing the filter
    // (visibleCityIds === null) zooms back out to the world view.
    effect(() => {
      const ids = this.visibleCityIds();
      if (!this.isMapReady) return;
      this.fitToFilter(ids);
    });
  }

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
    this.applyFilter();
  }

  private applyFilter(): void {
    if (!this.svgEl) return;
    const ids = this.visibleCityIds();
    const visibleSet = ids ? new Set(ids) : null;
    const svg = d3.select(this.svgEl.nativeElement);
    svg
      .select<SVGGElement>('.cce-world-map__cities')
      .selectAll<SVGGElement, AnyCity>('g.cce-city-marker')
      .classed('cce-city-marker--filtered-out', (d) => visibleSet !== null && !visibleSet.has(d.id));
    // City boundaries follow the same filter: hide discs whose city is
    // filtered out. data-city-id is set when the path is created.
    svg
      .select<SVGGElement>('.cce-world-map__city-boundaries')
      .selectAll<SVGPathElement, unknown>('path')
      .classed('cce-city-boundary--filtered-out', function () {
        const id = (this as SVGPathElement).getAttribute('data-city-id');
        return visibleSet !== null && id !== null && !visibleSet.has(id);
      });
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
    // Land clip needs to track the new projection too so per-city
    // boundary discs stay properly clipped after a resize.
    svg.selectAll<SVGPathElement, unknown>('#cce-land-clip path').attr('d', pathFn);
    this.repositionMarkers();
    this.rebuildBoundaries();
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

    // ─── Country labels at centroids ───────────────────────────────────
    // Major countries get larger labels (always visible). Smaller ones
    // get the --hidden class until zoom reveals them.
    //
    // For label positioning we use the centroid of the LARGEST sub-polygon
    // of each country (computed in projected pixel space). This matters
    // because:
    //   1. d3.geoCentroid() returns a geographic centroid which, when
    //      projected, can land outside the visible polygon (e.g. Russia's
    //      centroid is near the antimeridian, USA's centroid is skewed
    //      by Alaska, France's by overseas territories).
    //   2. d3.geoPath.centroid() on a MultiPolygon returns an area-weighted
    //      blend of all sub-polygon centroids — for the USA that's roughly
    //      the middle of the country (good), but for outliers the label
    //      can still drift.
    //   3. Using the centroid of the LARGEST sub-polygon places the label
    //      reliably inside the main visible mass.
    const pathInPx = d3.geoPath(this.projection);
    const labelFeatures = sorted
      .map((f) => {
        const id = String((f as { id?: string | number }).id ?? '');
        const name = COUNTRY_NAMES[id];
        if (!name) return null;
        const geom = (f as GeoFeature).geometry as
          | { type: 'Polygon'; coordinates: number[][][] }
          | { type: 'MultiPolygon'; coordinates: number[][][][] }
          | undefined;
        if (!geom) return null;
        // Pick the polygon (or sub-polygon of a MultiPolygon) with the
        // largest projected area, then take its projected centroid.
        let bestPolygon: number[][][] | null = null;
        let bestArea = -Infinity;
        const candidates =
          geom.type === 'Polygon'
            ? [geom.coordinates]
            : geom.type === 'MultiPolygon'
              ? geom.coordinates
              : [];
        for (const polyCoords of candidates) {
          const poly = { type: 'Polygon' as const, coordinates: polyCoords };
          const a = pathInPx.area(poly as never);
          if (a > bestArea) {
            bestArea = a;
            bestPolygon = polyCoords;
          }
        }
        if (!bestPolygon) return null;
        const [cx, cy] = pathInPx.centroid({
          type: 'Polygon',
          coordinates: bestPolygon,
        } as never);
        if (!Number.isFinite(cx) || !Number.isFinite(cy)) return null;
        // Total area for label-size decisions (Major / Hidden tiers).
        const area = pathInPx.area(f as never);
        return { id, name, x: cx, y: cy, area };
      })
      .filter((x): x is NonNullable<typeof x> => x !== null)
      .sort((a, b) => b.area - a.area);

    svg
      .select<SVGGElement>('.cce-world-map__country-labels')
      .selectAll('text')
      .data(labelFeatures)
      .enter()
      .append('text')
      .attr('x', (d) => d.x)
      .attr('y', (d) => d.y)
      .attr('class', (d) => {
        const major = d.area > 1500;
        const hiddenAtZoom1 = d.area < 200;
        return [
          'cce-country-label',
          major ? 'cce-country-label--major' : '',
          hiddenAtZoom1 ? 'cce-country-label--hidden' : '',
        ]
          .filter(Boolean)
          .join(' ');
      })
      .text((d) => d.name);

    // ─── Per-city geo-circle boundaries ─────────────────────────────────
    // Each city gets its OWN discrete circular boundary, sized by population
    // on a log scale so megacities get bigger circles than small ones
    // without crowding out neighbors. Boundaries are great-circle discs on
    // the sphere then projected, so they distort naturally with the map
    // projection. They're clipped to land via #cce-land-clip so the disc
    // doesn't bleed into the ocean for coastal cities.
    const sortedCities = ALL_CITIES.slice().sort((a, b) => b.population - a.population);
    // Population → boundary radius in DEGREES. Tuned so:
    //   100K   →  0.15° (~17 km)   small town footprint
    //   1M     →  0.45° (~50 km)   mid-size metro
    //   10M    →  0.75° (~83 km)   megacity metro
    //   37M    →  0.88° (~98 km)   Tokyo-tier
    const radiusDeg = (pop: number) =>
      Math.max(0.15, (Math.log10(Math.max(pop, 100000)) - 4) * 0.22);
    const cityBoundaries = sortedCities.map((city) => ({
      city,
      geometry: d3
        .geoCircle()
        .center([city.lon, city.lat])
        .radius(radiusDeg(city.population))(),
    }));

    // Populate land-clip with country shapes so per-city circles only
    // render over land.
    const landClip = svg.select<SVGClipPathElement>('#cce-land-clip');
    landClip.selectAll('path').remove();
    landClip
      .selectAll('path')
      .data(sorted)
      .enter()
      .append('path')
      .attr('d', pathFn);

    // Render the per-city boundary discs.
    svg
      .select<SVGGElement>('.cce-world-map__city-boundaries')
      .selectAll('path')
      .data(cityBoundaries)
      .enter()
      .append('path')
      .attr('data-city-id', (d) => d.city.id)
      .attr('class', (d) => `cce-city-boundary cce-city-boundary--${d.city.carbonTier}`)
      .attr('d', (d) => pathFn(d.geometry as never));

    // ─── City markers (featured + standard combined) ───────────────────
    const cityG = svg.select<SVGGElement>('.cce-world-map__cities');
    const markers = cityG
      .selectAll('g.cce-city-marker')
      .data(sortedCities, (d) => (d as AnyCity).id)
      .enter()
      .append('g')
      .attr('class', (d) => {
        const kind = (d as AnyCity).kind ?? 'featured';
        return `cce-city-marker cce-city-marker--${d.carbonTier} cce-city-marker--${kind}`;
      })
      .attr('data-city-id', (d) => d.id)
      .attr('transform', (d) => {
        const p = this.projection([d.lon, d.lat]);
        return p ? `translate(${p[0]}, ${p[1]})` : null;
      })
      .style('animation-delay', (_d, i) => `${i * 8 + 600}ms`)
      .on('click', (event: MouseEvent, d) => {
        event.stopPropagation();
        this.cityClicked.emit(d as AnyCity);
      })
      .on('mouseenter', (_event: MouseEvent, d) => this.setHover((d as AnyCity).id, true))
      .on('mouseleave', (_event: MouseEvent, d) => this.setHover((d as AnyCity).id, false));

    // Wrap visual elements in an INNER group. The outer marker `<g>` holds
    // the projected position (translate(px, py)) and is NEVER modified during
    // zoom, so pins stay glued to their cities. The inner `<g>` holds the
    // counter-scale (scale(1/k)) so circles/labels stay a constant visual size.
    const inner = markers.append('g').attr('class', 'cce-city-marker__inner');
    inner.append('circle').attr('class', 'cce-city-marker__pulse').attr('r', (d) => ((d as AnyCity).kind ?? 'featured') === 'featured' ? 5 : 3);
    inner.append('circle').attr('class', 'cce-city-marker__ring').attr('r', 8);
    inner.append('circle').attr('class', 'cce-city-marker__core').attr('r', (d) => ((d as AnyCity).kind ?? 'featured') === 'featured' ? 5 : 3);
    inner
      .append('text')
      .attr('class', 'cce-city-marker__label')
      .attr('y', -10)
      .attr('text-anchor', 'middle')
      .text((d) => d.name);

    // Pan + zoom.
    const ZOOM_REVEAL_STANDARD = 1.6;   // standard cities reveal at this zoom
    const ZOOM_REVEAL_LABELS_HIDDEN = 2.5; // hidden country labels reveal here
    this.zoom = d3
      .zoom<SVGSVGElement, unknown>()
      .scaleExtent([1, 12])
      .on('zoom', (event) => {
        d3.select(this.zoomGroupEl.nativeElement).attr('transform', event.transform.toString());
        const k = event.transform.k;

        // Counter-scale marker visuals so they stay readable when zoomed.
        // Apply scale to the INNER group only — the outer marker `<g>` keeps
        // its `translate(px, py)` (projected position) untouched, so pins
        // stay glued to their cities at every zoom level.
        d3.select(this.zoomGroupEl.nativeElement)
          .selectAll<SVGGElement, unknown>('g.cce-city-marker__inner')
          .attr('transform', `scale(${1 / k})`);

        // Counter-scale country labels (so they stay readable + don't bloat).
        d3.select(this.zoomGroupEl.nativeElement)
          .selectAll<SVGTextElement, { x: number; y: number }>('.cce-world-map__country-labels text')
          .each(function (d) {
            const node = this as SVGTextElement;
            node.setAttribute('transform', `scale(${1 / k}) translate(${d.x * (k - 1)} ${d.y * (k - 1)})`);
          });

        // Toggle: standard cities revealed at zoom > 1.6.
        const citiesG = d3.select(this.svgEl.nativeElement).select<SVGGElement>('.cce-world-map__cities');
        citiesG.classed('cce-cities--zoomed-in', k >= ZOOM_REVEAL_STANDARD);

        // Toggle: small-country labels revealed at zoom > 2.5.
        const labelsG = d3.select(this.svgEl.nativeElement).select<SVGGElement>('.cce-world-map__country-labels');
        labelsG.classed('cce-labels--zoomed-deep', k >= ZOOM_REVEAL_LABELS_HIDDEN);
      });
    svg.call(this.zoom);

    // Click on empty map area (ocean / country fill — anything that's not
    // a city pin) emits mapBackgroundClicked so the page can close the
    // detail panel. Pins call event.stopPropagation() in their own click
    // handler, so this only fires for non-pin clicks.
    svg.on('click', (event: MouseEvent) => {
      // d3.zoom triggers click events on drag-end too; require the click
      // to actually land on a non-pin element.
      const target = event.target as Element | null;
      if (target && target.closest('.cce-city-marker')) return;
      this.mapBackgroundClicked.emit();
    });

    this.applySelection();
    this.applyFilter();

    // Mark the map as ready so the visibleCityIds effect can start
    // running fit-to-filter animations. If a filter is already active
    // (e.g. user reloaded with state), apply it now.
    this.isMapReady = true;
    const initialFilter = this.visibleCityIds();
    if (initialFilter !== null) this.fitToFilter(initialFilter);
  }

  /**
   * Animate the D3 zoom transform so the bounding box of every city in
   * `ids` fits inside the SVG viewport with a comfortable margin. When
   * `ids` is `null` (no filter), zoom back out to the world view.
   *
   * Called from the constructor `effect()` whenever `visibleCityIds`
   * changes (which the parent page recomputes on any filter change).
   */
  private fitToFilter(ids: readonly string[] | null): void {
    if (!this.zoom || !this.svgEl?.nativeElement) return;
    const svg = d3.select(this.svgEl.nativeElement);
    if (ids === null) {
      // Filter cleared → reset to world view (zoomIdentity = scale 1, no
      // pan), animated.
      svg
        .transition()
        .duration(700)
        .ease(d3.easeCubicInOut)
        .call(this.zoom.transform, d3.zoomIdentity);
      return;
    }
    if (ids.length === 0) return; // nothing visible — leave map alone
    const idSet = new Set(ids);
    let minX = Infinity;
    let minY = Infinity;
    let maxX = -Infinity;
    let maxY = -Infinity;
    for (const c of ALL_CITIES) {
      if (!idSet.has(c.id)) continue;
      const p = this.projection([c.lon, c.lat]);
      if (!p) continue;
      if (p[0] < minX) minX = p[0];
      if (p[1] < minY) minY = p[1];
      if (p[0] > maxX) maxX = p[0];
      if (p[1] > maxY) maxY = p[1];
    }
    if (!Number.isFinite(minX)) return;

    const { width, height } = this.hostEl.nativeElement.getBoundingClientRect();
    const w = width || 1200;
    const h = height || 600;

    // For a single-city filter the bbox is a point; force a sensible
    // default zoom level (~k=4) so the user sees that city in context.
    let dx = maxX - minX;
    let dy = maxY - minY;
    if (dx < 1 && dy < 1) {
      // Single point — pick a fixed zoom level instead of computing scale.
      const k = 4;
      const cx = minX;
      const cy = minY;
      const tx = w / 2 - k * cx;
      const ty = h / 2 - k * cy;
      svg
        .transition()
        .duration(800)
        .ease(d3.easeCubicInOut)
        .call(this.zoom.transform, d3.zoomIdentity.translate(tx, ty).scale(k));
      return;
    }
    // Pad bounds so cities aren't flush against the viewport edge.
    const padX = Math.max(40, dx * 0.12);
    const padY = Math.max(40, dy * 0.12);
    minX -= padX;
    minY -= padY;
    maxX += padX;
    maxY += padY;
    dx = maxX - minX;
    dy = maxY - minY;

    // Scale to fit, clamped to the configured zoom extent [1, 12].
    let k = Math.min(w / dx, h / dy);
    k = Math.max(1, Math.min(12, k));
    const cx = (minX + maxX) / 2;
    const cy = (minY + maxY) / 2;
    const tx = w / 2 - k * cx;
    const ty = h / 2 - k * cy;
    svg
      .transition()
      .duration(800)
      .ease(d3.easeCubicInOut)
      .call(this.zoom.transform, d3.zoomIdentity.translate(tx, ty).scale(k));
  }

  private repositionMarkers(): void {
    const cityG = d3.select(this.svgEl.nativeElement).select<SVGGElement>('.cce-world-map__cities');
    cityG.selectAll<SVGGElement, AnyCity>('g.cce-city-marker').attr('transform', (d) => {
      const p = this.projection([d.lon, d.lat]);
      return p ? `translate(${p[0]}, ${p[1]})` : null;
    });
  }

  private applySelection(): void {
    const id = this.selectedCityId();
    if (id === this.currentSelectedId) return;
    this.currentSelectedId = id;
    const svg = d3.select(this.svgEl.nativeElement);
    svg
      .select<SVGGElement>('.cce-world-map__cities')
      .selectAll<SVGGElement, AnyCity>('g.cce-city-marker')
      .classed('cce-city-marker--selected', (d) => d.id === id);
    // Highlight the selected city's boundary disc too — fill goes from
    // 14% to 42% opacity, stroke goes from 0.8 to 1.8 px white.
    svg
      .select<SVGGElement>('.cce-world-map__city-boundaries')
      .selectAll<SVGPathElement, unknown>('path')
      .classed('cce-city-boundary--selected', function () {
        return (this as SVGPathElement).getAttribute('data-city-id') === id;
      });
  }

  /**
   * Set/clear the hover highlight on the matching boundary disc when a
   * marker is hovered. Called from `mouseenter`/`mouseleave` handlers
   * wired up in {@link render}.
   */
  private setHover(cityId: string, on: boolean): void {
    if (!this.svgEl) return;
    d3.select(this.svgEl.nativeElement)
      .select<SVGGElement>('.cce-world-map__city-boundaries')
      .selectAll<SVGPathElement, unknown>('path')
      .filter(function () {
        return (this as SVGPathElement).getAttribute('data-city-id') === cityId;
      })
      .classed('cce-city-boundary--hover', on);
  }

  /**
   * Recompute every per-city boundary geometry from the current
   * projection and rewrite the `<path>` `d` attributes. Called from
   * {@link handleResize} after the projection has been refit.
   */
  private rebuildBoundaries(): void {
    const svg = d3.select(this.svgEl.nativeElement);
    const pathFn = (d: unknown) => this.path(d as never) ?? '';
    const radiusDeg = (pop: number) =>
      Math.max(0.15, (Math.log10(Math.max(pop, 100000)) - 4) * 0.22);
    svg
      .select<SVGGElement>('.cce-world-map__city-boundaries')
      .selectAll<SVGPathElement, unknown>('path')
      .each(function () {
        const node = this as SVGPathElement;
        const id = node.getAttribute('data-city-id');
        const city = ALL_CITIES.find((c) => c.id === id);
        if (!city) return;
        const geom = d3
          .geoCircle()
          .center([city.lon, city.lat])
          .radius(radiusDeg(city.population))();
        node.setAttribute('d', pathFn(geom as never));
      });
  }
}
