import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, provideRouter } from '@angular/router';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { signal } from '@angular/core';
import { LocaleService } from '@frontend/i18n';
import { ToastService } from '@frontend/ui-kit';
import { TranslateModule } from '@ngx-translate/core';
import { KnowledgeApiService, type Result } from './knowledge-api.service';
import type { Resource } from './knowledge.types';
import { ResourceDetailPage } from './resource-detail.page';

const SAMPLE: Resource = {
  id: 'r1',
  titleAr: 'عنوان', titleEn: 'Title',
  resourceType: 'Pdf',
  categoryId: 'cat-1',
  countryId: null,
  publishedOn: '2026-04-29',
  viewCount: 42,
  descriptionAr: 'وصف عربي',
  descriptionEn: 'English description',
  uploadedById: 'admin',
  assetFileId: 'asset-1',
  isCenterManaged: true,
};

describe('ResourceDetailPage', () => {
  let fixture: ComponentFixture<ResourceDetailPage>;
  let page: ResourceDetailPage;
  let getResource: jest.Mock;
  let download: jest.Mock;
  let toast: { success: jest.Mock; error: jest.Mock };
  let localeSig: ReturnType<typeof signal<'ar' | 'en'>>;

  function ok<T>(value: T): Result<T> { return { ok: true, value }; }

  beforeEach(async () => {
    getResource = jest.fn().mockResolvedValue(ok(SAMPLE));
    download = jest.fn();
    toast = { success: jest.fn(), error: jest.fn() };
    localeSig = signal<'ar' | 'en'>('en');

    await TestBed.configureTestingModule({
      imports: [ResourceDetailPage, TranslateModule.forRoot()],
      providers: [
        provideRouter([]),
        provideNoopAnimations(),
        { provide: KnowledgeApiService, useValue: { getResource, download } },
        { provide: ToastService, useValue: toast },
        { provide: LocaleService, useValue: { locale: localeSig.asReadonly() } },
        { provide: ActivatedRoute, useValue: { snapshot: { paramMap: { get: () => 'r1' } } } },
      ],
    }).compileComponents();
    fixture = TestBed.createComponent(ResourceDetailPage);
    page = fixture.componentInstance;
  });

  it('loads resource on init from route id', async () => {
    fixture.detectChanges();
    await fixture.whenStable();
    expect(getResource).toHaveBeenCalledWith('r1');
    expect(page.resource()).toEqual(SAMPLE);
  });

  it('sets errorKind on 404', async () => {
    getResource.mockResolvedValueOnce({ ok: false, error: { kind: 'not-found' } });
    fixture.detectChanges();
    await fixture.whenStable();
    expect(page.errorKind()).toBe('not-found');
  });

  it('locale toggle updates title/description computed', async () => {
    fixture.detectChanges();
    await fixture.whenStable();
    expect(page.title()).toBe('Title');
    expect(page.description()).toBe('English description');
    localeSig.set('ar');
    expect(page.title()).toBe('عنوان');
    expect(page.description()).toBe('وصف عربي');
  });

  it('download button materializes blob and toasts success', async () => {
    fixture.detectChanges();
    await fixture.whenStable();
    download.mockResolvedValueOnce(ok(new Blob(['x'], { type: 'application/pdf' })));
    Object.defineProperty(URL, 'createObjectURL', { value: () => 'blob:x', configurable: true });
    Object.defineProperty(URL, 'revokeObjectURL', { value: jest.fn(), configurable: true });
    const a = document.createElement('a');
    Object.defineProperty(a, 'click', { value: jest.fn() });
    jest.spyOn(document, 'createElement').mockReturnValueOnce(a);

    await page.download();

    expect(download).toHaveBeenCalledWith('r1');
    expect(toast.success).toHaveBeenCalledWith('resources.download.toast');
  });

  it('download error surfaces via toast.error', async () => {
    fixture.detectChanges();
    await fixture.whenStable();
    download.mockResolvedValueOnce({ ok: false, error: { kind: 'forbidden' } });
    await page.download();
    expect(toast.error).toHaveBeenCalledWith('errors.forbidden');
    expect(toast.success).not.toHaveBeenCalled();
  });
});
