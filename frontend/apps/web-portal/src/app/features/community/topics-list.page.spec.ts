import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { signal } from '@angular/core';
import { LocaleService } from '@frontend/i18n';
import { TranslateModule } from '@ngx-translate/core';
import { CommunityApiService, type Result } from './community-api.service';
import type { PublicTopic } from './community.types';
import { TopicsListPage } from './topics-list.page';

const T1: PublicTopic = {
  id: 't1',
  nameAr: 'موضوع 1', nameEn: 'Topic One',
  descriptionAr: 'وصف', descriptionEn: 'Desc 1',
  slug: 'one',
  parentId: null, iconUrl: null, orderIndex: 2,
};
const T2: PublicTopic = {
  ...T1, id: 't2', nameEn: 'Topic Two', nameAr: 'موضوع 2', slug: 'two', orderIndex: 1,
};

function ok<T>(value: T): Result<T> {
  return { ok: true, value };
}

describe('TopicsListPage', () => {
  let fixture: ComponentFixture<TopicsListPage>;
  let page: TopicsListPage;
  let listTopics: jest.Mock;
  let localeSig: ReturnType<typeof signal<'ar' | 'en'>>;

  beforeEach(async () => {
    listTopics = jest.fn().mockResolvedValue(ok([T1, T2]));
    localeSig = signal<'ar' | 'en'>('en');

    await TestBed.configureTestingModule({
      imports: [TopicsListPage, TranslateModule.forRoot()],
      providers: [
        provideRouter([]),
        provideNoopAnimations(),
        { provide: CommunityApiService, useValue: { listTopics } },
        { provide: LocaleService, useValue: { locale: localeSig.asReadonly() } },
      ],
    }).compileComponents();
    fixture = TestBed.createComponent(TopicsListPage);
    page = fixture.componentInstance;
  });

  it('init load renders one card per topic, sorted by orderIndex ascending', async () => {
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
    expect(listTopics).toHaveBeenCalled();
    const sorted = page.sortedRows();
    expect(sorted.map((t) => t.id)).toEqual(['t2', 't1']);
    expect(fixture.nativeElement.querySelectorAll('cce-topic-card')).toHaveLength(2);
  });

  it('card title localizes when locale toggles to ar', async () => {
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
    let html = fixture.nativeElement.textContent ?? '';
    expect(html).toContain('Topic One');
    localeSig.set('ar');
    fixture.detectChanges();
    html = fixture.nativeElement.textContent ?? '';
    expect(html).toContain('موضوع 1');
  });

  it('empty result triggers empty() computed and renders the empty message', async () => {
    listTopics.mockResolvedValueOnce(ok([]));
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
    expect(page.empty()).toBe(true);
    const html = fixture.nativeElement.textContent ?? '';
    expect(html).toContain('community.empty.topics');
  });

  it('error path renders error banner; retry triggers fresh listTopics', async () => {
    listTopics.mockResolvedValueOnce({ ok: false, error: { kind: 'server' } });
    fixture.detectChanges();
    await fixture.whenStable();
    expect(page.errorKind()).toBe('server');
    listTopics.mockClear();
    listTopics.mockResolvedValueOnce(ok([T1]));
    page.retry();
    await Promise.resolve();
    expect(listTopics).toHaveBeenCalled();
  });

  it('card routerLink points to /community/topics/{slug}', async () => {
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
    const links = fixture.nativeElement.querySelectorAll('a.cce-topic-card') as NodeListOf<HTMLAnchorElement>;
    expect(links).toHaveLength(2);
    const hrefs = Array.from(links).map((a) => a.getAttribute('href')).sort();
    expect(hrefs).toEqual(['/community/topics/one', '/community/topics/two']);
  });
});
