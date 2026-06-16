import {
  AfterViewInit,
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  NgZone,
  OnChanges,
  OnDestroy,
  SimpleChanges,
  inject,
  input,
  output,
  viewChild,
} from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import * as d3 from 'd3';
import * as topojson from 'topojson-client';
import type { Country } from './country.types';

const COUNTRY_COORDS: Record<string, [number, number]> = {
  SA: [45.0, 24.0],
  AE: [54.0, 24.0],
  EG: [30.0, 27.0],
  JO: [36.3, 31.0],
  BH: [50.6, 26.0],
  KW: [47.7, 29.3],
  OM: [57.5, 21.5],
  QA: [51.2, 25.4],
  MA: [-7.0, 31.8],
  DZ: [3.0, 28.0],
  TN: [9.0, 34.0],
  LY: [17.0, 27.0],
  SD: [30.0, 15.0],
  IQ: [44.0, 33.0],
  SY: [38.0, 35.0],
  LB: [35.9, 33.9],
  YE: [48.5, 15.5],
  TR: [35.2, 39.0],
  IR: [53.7, 32.4],
  PK: [69.3, 30.4],
  IN: [78.9, 20.6],
  CN: [104.2, 35.9],
  JP: [138.3, 36.2],
  ID: [113.9, -0.8],
  AU: [133.8, -25.3],
  ZA: [25.1, -29.0],
  NG: [8.7, 9.1],
  KE: [37.9, -0.02],
  BR: [-51.9, -14.2],
  MX: [-102.6, 23.6],
  US: [-95.7, 37.1],
  CA: [-96.8, 56.1],
  DE: [10.5, 51.2],
  FR: [2.2, 46.2],
  GB: [-3.4, 55.4],
  IT: [12.6, 41.9],
  ES: [-3.7, 40.4],
  RU: [60.0, 55.0],
};

@Component({
  selector: 'cce-countries-world-map',
  standalone: true,
  template: `
    <div class="cce-world-map" #mapContainer>
      <svg #mapSvg class="cce-world-map__svg"></svg>
    </div>
  `,
  styles: [`
    :host { display: block; width: 100%; }
    .cce-world-map { width: 100%; position: relative; }
    .cce-world-map__svg { width: 100%; height: auto; display: block; }
    :host ::ng-deep .cce-dot {
      cursor: pointer;
      fill: var(--color-text-primary);
      stroke: var(--white);
      stroke-width: 1.5;
      transition: fill 0.18s ease, r 0.18s ease;
    }
    :host ::ng-deep .cce-dot:hover, :host ::ng-deep .cce-dot--active {
      fill: var(--color-brand);
      stroke: var(--color-brand-accent);
    }
    :host ::ng-deep .cce-world-path {
      fill: var(--neutrals--300);
      stroke: var(--white);
      stroke-width: 0.4;
    }
  `],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CountriesWorldMapComponent implements AfterViewInit, OnChanges, OnDestroy {
  private readonly http = inject(HttpClient);
  private readonly zone = inject(NgZone);

  readonly countries = input<Country[]>([]);
  readonly selectedId = input<string | null>(null);
  readonly countrySelected = output<Country>();

  readonly mapSvg = viewChild.required<ElementRef<SVGElement>>('mapSvg');
  readonly mapContainer = viewChild.required<ElementRef<HTMLDivElement>>('mapContainer');

  private projection!: d3.GeoProjection;
  private pathGen!: d3.GeoPath;
  private svg!: d3.Selection<SVGElement, unknown, null, undefined>;
  private initialized = false;
  private resizeObserver?: ResizeObserver;

  async ngAfterViewInit(): Promise<void> {
    await this.initMap();
    this.resizeObserver = new ResizeObserver(() => this.zone.run(() => this.resize()));
    this.resizeObserver.observe(this.mapContainer().nativeElement);
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (!this.initialized) return;
    if (changes['countries']) this.renderDots();
    if (changes['selectedId']) this.updateActiveState();
  }

  ngOnDestroy(): void {
    this.resizeObserver?.disconnect();
  }

  private async initMap(): Promise<void> {
    const containerEl = this.mapContainer().nativeElement;
    const width = containerEl.offsetWidth || 900;
    const height = Math.round(width * 0.62);

    const svgEl = this.mapSvg().nativeElement;
    this.svg = d3.select(svgEl);
    this.svg.attr('viewBox', `0 0 ${width} ${height}`).attr('preserveAspectRatio', 'xMidYMid meet');

    this.projection = d3.geoNaturalEarth1()
      .scale(width / 5.8)
      .translate([width / 2, height / 2]);
    this.pathGen = d3.geoPath().projection(this.projection);

    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    const world = await firstValueFrom(this.http.get<any>('/assets/world-110m.json'));
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    const countriesGeo = topojson.feature(world, world.objects['countries'] as any) as any;
    const features: unknown[] = countriesGeo.features ?? [];

    this.svg.append('g').attr('class', 'cce-world-paths')
      .selectAll('path')
      .data(features)
      .join('path')
      .attr('class', 'cce-world-path')
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      .attr('d', (f: any) => this.pathGen(f) ?? '');

    this.svg.append('g').attr('class', 'cce-dots');

    this.initialized = true;
    this.renderDots();
  }

  private renderDots(): void {
    if (!this.initialized) return;
    const dotsGroup = this.svg.select('g.cce-dots');

    const dots = dotsGroup.selectAll<SVGCircleElement, Country>('circle.cce-dot')
      .data(this.countries().filter(c => !!COUNTRY_COORDS[c.isoAlpha2]), (d: Country) => d.id);

    dots.enter()
      .append('circle')
      .attr('class', 'cce-dot')
      .attr('r', 5)
      .attr('cx', (c) => (this.projection(COUNTRY_COORDS[c.isoAlpha2]) ?? [0, 0])[0])
      .attr('cy', (c) => (this.projection(COUNTRY_COORDS[c.isoAlpha2]) ?? [0, 0])[1])
      .on('click', (event, c) => this.zone.run(() => this.countrySelected.emit(c)));

    dots.exit().remove();

    this.updateActiveState();
  }

  private updateActiveState(): void {
    if (!this.initialized) return;
    const sel = this.selectedId();
    this.svg.selectAll<SVGCircleElement, Country>('circle.cce-dot')
      .classed('cce-dot--active', (c) => c.id === sel)
      .attr('r', (c) => c.id === sel ? 7 : 5);
  }

  private resize(): void {
    if (!this.initialized) return;
    const width = this.mapContainer().nativeElement.offsetWidth;
    if (!width) return;
    const height = Math.round(width * 0.62);
    this.svg.attr('viewBox', `0 0 ${width} ${height}`);
    this.projection.scale(width / 5.8).translate([width / 2, height / 2]);
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    this.svg.selectAll<SVGPathElement, any>('path.cce-world-path')
      .attr('d', (f) => this.pathGen(f) ?? '');
    this.svg.selectAll<SVGCircleElement, Country>('circle.cce-dot')
      .attr('cx', (c) => (this.projection(COUNTRY_COORDS[c.isoAlpha2]) ?? [0, 0])[0])
      .attr('cy', (c) => (this.projection(COUNTRY_COORDS[c.isoAlpha2]) ?? [0, 0])[1]);
  }
}
