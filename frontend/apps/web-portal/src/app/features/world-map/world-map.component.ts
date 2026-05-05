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
        </defs>
        <rect class="cce-world-map__ocean" width="100%" height="100%" fill="url(#oceanGradient)" />
        <g #zoomGroup class="cce-world-map__zoom-group">
          <g class="cce-world-map__graticule"></g>
          <g class="cce-world-map__countries"></g>
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

      /* Standard cities (zoom-revealed) — initially hidden, reveal at zoom > threshold via JS class. */
      :host ::ng-deep .cce-world-map__cities .cce-city-marker--standard {
        opacity: 0;
        pointer-events: none;
        transition: opacity 0.3s ease;
      }
      :host ::ng-deep .cce-world-map__cities.cce-cities--zoomed-in .cce-city-marker--standard {
        opacity: 1;
        pointer-events: auto;
      }
      /* Standard markers smaller + dimmer. */
      :host ::ng-deep .cce-world-map__cities .cce-city-marker--standard .cce-city-marker__core {
        stroke-width: 0.8;
      }
      :host ::ng-deep .cce-world-map__cities .cce-city-marker--standard .cce-city-marker__pulse {
        animation: none;
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
  @Output() readonly cityClicked = new EventEmitter<AnyCity>();

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

    // ─── Country labels at centroids ───────────────────────────────────
    // Major countries get larger labels (always visible). Smaller ones
    // get the --hidden class until zoom reveals them.
    const labelFeatures = sorted
      .map((f) => {
        const id = String((f as { id?: string | number }).id ?? '');
        const name = COUNTRY_NAMES[id];
        if (!name) return null;
        const centroid = d3.geoCentroid(f as never);
        const projected = this.projection(centroid);
        if (!projected) return null;
        // Approximate area of the country path in pixel-space (used for
        // sizing decision). geoPath.area returns the projected area in
        // square pixels.
        const area = d3.geoPath(this.projection).area(f as never);
        return { id, name, x: projected[0], y: projected[1], area };
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

    // ─── City markers (featured + standard combined) ───────────────────
    const sortedCities = ALL_CITIES.slice().sort((a, b) => b.population - a.population);
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
      });

    markers.append('circle').attr('class', 'cce-city-marker__pulse').attr('r', (d) => ((d as AnyCity).kind ?? 'featured') === 'featured' ? 5 : 3);
    markers.append('circle').attr('class', 'cce-city-marker__ring').attr('r', 8);
    markers.append('circle').attr('class', 'cce-city-marker__core').attr('r', (d) => ((d as AnyCity).kind ?? 'featured') === 'featured' ? 5 : 3);
    markers
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
        d3.select(this.zoomGroupEl.nativeElement)
          .selectAll<SVGGElement, AnyCity>('g.cce-city-marker')
          .each(function () {
            const node = this as SVGGElement;
            const t = node.getAttribute('transform') ?? '';
            const baseTranslate = t.replace(/\s*scale\([^)]*\)/, '');
            node.setAttribute('transform', `${baseTranslate} scale(${1 / k})`);
          });

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

    this.applySelection();
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
    d3.select(this.svgEl.nativeElement)
      .select<SVGGElement>('.cce-world-map__cities')
      .selectAll<SVGGElement, AnyCity>('g.cce-city-marker')
      .classed('cce-city-marker--selected', (d) => d.id === id);
  }
}
