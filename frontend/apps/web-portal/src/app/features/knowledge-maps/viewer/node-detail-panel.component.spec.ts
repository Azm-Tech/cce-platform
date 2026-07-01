import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { TranslocoTestingModule } from '@jsverse/transloco';
import type { InteractiveMapNode, NodeDetails } from '../knowledge-maps.types';
import { NodeDetailPanelComponent } from './node-detail-panel.component';

const PARENT: InteractiveMapNode = {
  id: 'p1',
  nameAr: 'التقليل', nameEn: 'Reduce',
  iconKey: 'recycle',
  level: 0,
  parentId: null,
  topicId: 't1',
  tags: [],
};
const NODE: InteractiveMapNode = {
  ...PARENT,
  id: 'n1',
  nameAr: 'كفاءة الطاقة', nameEn: 'Energy Efficiency',
  iconKey: 'factory',
  level: 1,
  parentId: 'p1',
};

const DETAILS: NodeDetails = {
  node: { id: 'n1', nameAr: 'كفاءة الطاقة', nameEn: 'Energy Efficiency', iconKey: 'factory', topicId: 't1' },
  topic: {
    id: 't1',
    nameAr: 'عام', nameEn: 'General',
    descriptionAr: 'وصف عربي', descriptionEn: 'A topic description',
    slug: 'general',
  },
  resources: [
    {
      id: 'r1',
      titleAr: 'مصدر', titleEn: 'Source One',
      resourceType: 'paper',
      categoryNameAr: 'فئة', categoryNameEn: 'Category',
      publishedOn: '2026-01-10T00:00:00Z',
    },
  ],
  news: [
    { id: 'nw1', titleAr: 'خبر', titleEn: 'News One', publishedOn: '2026-02-01T00:00:00Z' },
  ],
  events: [
    {
      id: 'ev1',
      titleAr: 'فعالية', titleEn: 'Event One',
      startsOn: '2026-03-01T00:00:00Z',
      endsOn: '2026-03-02T00:00:00Z',
    },
  ],
  posts: [
    {
      id: 'po1',
      type: 'question',
      title: 'A question post',
      content: 'body',
      commentsCount: 3,
      createdOn: '2026-04-01T00:00:00Z',
    },
  ],
};

describe('NodeDetailPanelComponent', () => {
  let fixture: ComponentFixture<NodeDetailPanelComponent>;
  let component: NodeDetailPanelComponent;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [
        NodeDetailPanelComponent,
        TranslocoTestingModule.forRoot({
          langs: { en: {}, ar: {} },
          translocoConfig: { availableLangs: ['en', 'ar'], defaultLang: 'en' },
        }),
      ],
      providers: [provideNoopAnimations()],
    }).compileComponents();

    fixture = TestBed.createComponent(NodeDetailPanelComponent);
    component = fixture.componentInstance;
  });

  it('renders nothing when node input is null', () => {
    fixture.componentRef.setInput('node', null);
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('.cce-node-detail')).toBeNull();
  });

  it('renders title and parent eyebrow when node + allNodes set', () => {
    fixture.componentRef.setInput('node', NODE);
    fixture.componentRef.setInput('allNodes', [PARENT, NODE]);
    fixture.componentRef.setInput('locale', 'en');
    fixture.detectChanges();
    expect(component.name()).toBe('Energy Efficiency');
    expect(component.parentName()).toBe('Reduce');
    const text = fixture.nativeElement.textContent ?? '';
    expect(text).toContain('Energy Efficiency');
    expect(text).toContain('Reduce');
  });

  it('shows the loading state when loading and no details', () => {
    fixture.componentRef.setInput('node', NODE);
    fixture.componentRef.setInput('loading', true);
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('.cce-node-detail__loading')).not.toBeNull();
  });

  it('renders a resource row and emits linkActivated(resource) on click', () => {
    fixture.componentRef.setInput('node', NODE);
    fixture.componentRef.setInput('details', DETAILS);
    fixture.componentRef.setInput('locale', 'en');
    fixture.detectChanges();
    let emitted: { kind: string; id: string } | null = null;
    component.linkActivated.subscribe((e) => { emitted = e; });
    const rows = fixture.nativeElement.querySelectorAll(
      '.cce-node-detail__section .cce-node-detail__row',
    ) as NodeListOf<HTMLButtonElement>;
    expect(rows.length).toBeGreaterThan(0);
    rows[0].click();
    expect(emitted).toEqual({ kind: 'resource', id: 'r1' });
  });

  it('merges news + events and emits linkActivated(news) on the first news row', () => {
    fixture.componentRef.setInput('node', NODE);
    fixture.componentRef.setInput('details', DETAILS);
    fixture.detectChanges();
    expect(component.newsEvents().length).toBe(2);
    expect(component.newsEvents()[0]).toEqual(
      expect.objectContaining({ kind: 'news', id: 'nw1' }),
    );
    let emitted: { kind: string; id: string } | null = null;
    component.linkActivated.subscribe((e) => { emitted = e; });
    component.onLink('news', 'nw1');
    expect(emitted).toEqual({ kind: 'news', id: 'nw1' });
  });

  it('caps resources and news+events to 3 each', () => {
    const many: NodeDetails = {
      ...DETAILS,
      resources: Array.from({ length: 5 }, (_, i) => ({
        id: `r${i}`,
        titleAr: 'مصدر', titleEn: `Source ${i}`,
        resourceType: 'paper',
        categoryNameAr: 'فئة', categoryNameEn: 'Category',
        publishedOn: '2026-01-10T00:00:00Z',
      })),
      news: Array.from({ length: 5 }, (_, i) => ({
        id: `nw${i}`,
        titleAr: 'خبر', titleEn: `News ${i}`,
        publishedOn: '2026-02-01T00:00:00Z',
      })),
      events: Array.from({ length: 5 }, (_, i) => ({
        id: `ev${i}`,
        titleAr: 'فعالية', titleEn: `Event ${i}`,
        startsOn: '2026-03-01T00:00:00Z',
        endsOn: '2026-03-02T00:00:00Z',
      })),
    };
    fixture.componentRef.setInput('node', NODE);
    fixture.componentRef.setInput('details', many);
    fixture.detectChanges();
    expect(component.resources().length).toBe(3);
    // 3 news + 3 events merged.
    expect(component.newsEvents().length).toBe(6);
    expect(component.newsEvents().filter((r) => r.kind === 'news').length).toBe(3);
    expect(component.newsEvents().filter((r) => r.kind === 'event').length).toBe(3);
  });

  it('renders post rows and emits linkActivated(post) on click', () => {
    fixture.componentRef.setInput('node', NODE);
    fixture.componentRef.setInput('details', DETAILS);
    fixture.detectChanges();
    let emitted: { kind: string; id: string } | null = null;
    component.linkActivated.subscribe((e) => { emitted = e; });
    component.onLink('post', 'po1');
    expect(emitted).toEqual({ kind: 'post', id: 'po1' });
  });

  it('close button click emits (closed)', () => {
    fixture.componentRef.setInput('node', NODE);
    fixture.detectChanges();
    let closed = false;
    component.closed.subscribe(() => { closed = true; });
    const btn = fixture.nativeElement.querySelector('.cce-node-detail__close') as HTMLButtonElement;
    btn.click();
    expect(closed).toBe(true);
  });

  it('ESC emits (closed) only when a node is visible', () => {
    fixture.componentRef.setInput('node', null);
    fixture.detectChanges();
    let closed = false;
    component.closed.subscribe(() => { closed = true; });
    component.onEscape();
    expect(closed).toBe(false);

    fixture.componentRef.setInput('node', NODE);
    fixture.detectChanges();
    component.onEscape();
    expect(closed).toBe(true);
  });

  it('safely handles missing topic and posts in details, using node fallbacks', () => {
    const detailsWithoutTopic: NodeDetails = {
      node: {
        id: 'n1',
        nameAr: 'كفاءة الطاقة',
        nameEn: 'Energy Efficiency',
        iconKey: 'factory',
        topicId: 't1',
        titleAr: 'عنوان بديل',
        titleEn: 'Alternative Title',
        descriptionAr: 'وصف بديل للطرفيات',
        descriptionEn: 'Alternative description from node',
      },
      resources: [],
      news: [],
      events: [],
    };
    
    fixture.componentRef.setInput('node', NODE);
    fixture.componentRef.setInput('details', detailsWithoutTopic);
    fixture.componentRef.setInput('locale', 'en');
    fixture.detectChanges();

    expect(component.description()).toBe('Alternative description from node');
    expect(component.topicName()).toBe('Alternative Title');
    expect(component.posts()).toEqual([]);
    
    // Switch to Arabic
    fixture.componentRef.setInput('locale', 'ar');
    fixture.detectChanges();
    expect(component.description()).toBe('وصف بديل للطرفيات');
    expect(component.topicName()).toBe('عنوان بديل');
  });
});
