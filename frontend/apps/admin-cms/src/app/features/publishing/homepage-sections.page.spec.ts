import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { TranslateModule } from '@ngx-translate/core';
import { AuthService } from '../../core/auth/auth.service';
import { ConfirmDialogService, ToastService } from '@frontend/ui-kit';
import { PublishingApiService, type Result } from './publishing-api.service';
import type { HomepageSection } from './publishing.types';
import { HomepageSectionsPage } from './homepage-sections.page';

const S1: HomepageSection = {
  id: 'h1', sectionType: 'Hero', orderIndex: 0,
  contentAr: 'a-ar', contentEn: 'a-en', isActive: true,
};
const S2: HomepageSection = {
  id: 'h2', sectionType: 'FeaturedNews', orderIndex: 1,
  contentAr: 'b-ar', contentEn: 'b-en', isActive: true,
};

describe('HomepageSectionsPage', () => {
  let fixture: ComponentFixture<HomepageSectionsPage>;
  let page: HomepageSectionsPage;
  let listHomepageSections: jest.Mock;
  let createHomepageSection: jest.Mock;
  let updateHomepageSection: jest.Mock;
  let deleteHomepageSection: jest.Mock;
  let reorderHomepageSections: jest.Mock;
  let toast: { success: jest.Mock; error: jest.Mock };
  let confirm: { confirm: jest.Mock };

  function ok<T>(value: T): Result<T> { return { ok: true, value }; }

  beforeEach(async () => {
    listHomepageSections = jest.fn().mockResolvedValue(ok([S1, S2]));
    createHomepageSection = jest.fn().mockResolvedValue(ok({ ...S1, id: 'h3' }));
    updateHomepageSection = jest.fn().mockResolvedValue(ok(S1));
    deleteHomepageSection = jest.fn().mockResolvedValue({ ok: true, value: undefined });
    reorderHomepageSections = jest.fn().mockResolvedValue({ ok: true, value: undefined });
    toast = { success: jest.fn(), error: jest.fn() };
    confirm = { confirm: jest.fn().mockResolvedValue(true) };

    await TestBed.configureTestingModule({
      imports: [HomepageSectionsPage, TranslateModule.forRoot()],
      providers: [
        provideNoopAnimations(),
        provideHttpClient(),
        provideHttpClientTesting(),
        {
          provide: PublishingApiService,
          useValue: {
            listHomepageSections,
            createHomepageSection,
            updateHomepageSection,
            deleteHomepageSection,
            reorderHomepageSections,
          },
        },
        { provide: ToastService, useValue: toast },
        { provide: ConfirmDialogService, useValue: confirm },
        { provide: AuthService, useValue: { currentUser: () => null, isAuthenticated: () => false, hasPermission: () => true } },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(HomepageSectionsPage);
    page = fixture.componentInstance;
  });

  it('loads + sorts by orderIndex', async () => {
    fixture.detectChanges();
    await fixture.whenStable();
    expect(page.rows().map((s) => s.id)).toEqual(['h1', 'h2']);
  });

  it('create POSTs new section with orderIndex = current length', async () => {
    fixture.detectChanges();
    await fixture.whenStable();
    page.newSectionType.set('UpcomingEvents');
    page.newContentAr.set('ar');
    page.newContentEn.set('en');
    await page.create();
    expect(createHomepageSection).toHaveBeenCalledWith({
      sectionType: 'UpcomingEvents',
      orderIndex: 2,
      contentAr: 'ar',
      contentEn: 'en',
    });
    expect(toast.success).toHaveBeenCalledWith('homepage.create.toast');
  });

  it('update PUTs with merged patch values', async () => {
    fixture.detectChanges();
    await fixture.whenStable();
    await page.update(S1, { contentAr: 'new-ar' });
    expect(updateHomepageSection).toHaveBeenCalledWith('h1', {
      contentAr: 'new-ar',
      contentEn: 'a-en',
      isActive: true,
    });
  });

  it('moveDown reorders + POSTs new sequence', async () => {
    fixture.detectChanges();
    await fixture.whenStable();
    await page.moveDown(S1);
    expect(reorderHomepageSections).toHaveBeenCalledWith({
      assignments: [
        { id: 'h2', orderIndex: 0 },
        { id: 'h1', orderIndex: 1 },
      ],
    });
    expect(toast.success).toHaveBeenCalledWith('homepage.reorder.toast');
  });

  it('moveUp at top is a no-op', async () => {
    fixture.detectChanges();
    await fixture.whenStable();
    await page.moveUp(S1);
    expect(reorderHomepageSections).not.toHaveBeenCalled();
  });

  it('delete confirms then DELETEs', async () => {
    fixture.detectChanges();
    await fixture.whenStable();
    await page.delete(S1);
    expect(deleteHomepageSection).toHaveBeenCalledWith('h1');
    expect(toast.success).toHaveBeenCalledWith('homepage.delete.toast');
  });
});
