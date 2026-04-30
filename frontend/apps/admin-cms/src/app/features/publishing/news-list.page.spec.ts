import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { MatDialog } from '@angular/material/dialog';
import { TranslateModule } from '@ngx-translate/core';
import { of } from 'rxjs';
import { AuthService } from '../../core/auth/auth.service';
import { ConfirmDialogService, ToastService } from '@frontend/ui-kit';
import { PublishingApiService, type Result } from './publishing-api.service';
import type { News, PagedResult } from './publishing.types';
import { NewsListPage } from './news-list.page';

const NEWS: News = {
  id: 'n1',
  titleAr: 't-ar', titleEn: 't-en',
  contentAr: 'c-ar', contentEn: 'c-en',
  slug: 's', authorId: 'a',
  featuredImageUrl: null,
  publishedOn: null,
  isFeatured: false, isPublished: false,
  rowVersion: 'v',
};

describe('NewsListPage', () => {
  let fixture: ComponentFixture<NewsListPage>;
  let page: NewsListPage;
  let listNews: jest.Mock;
  let publishNews: jest.Mock;
  let deleteNews: jest.Mock;
  let confirm: { confirm: jest.Mock };
  let toast: { success: jest.Mock; error: jest.Mock };
  let dialog: { open: jest.Mock };
  let dialogRef: { afterClosed: jest.Mock };

  function ok(value: PagedResult<News>): Result<PagedResult<News>> {
    return { ok: true, value };
  }

  beforeEach(async () => {
    listNews = jest.fn().mockResolvedValue(ok({ items: [NEWS], page: 1, pageSize: 20, total: 1 }));
    publishNews = jest.fn().mockResolvedValue({ ok: true, value: { ...NEWS, isPublished: true } });
    deleteNews = jest.fn().mockResolvedValue({ ok: true, value: undefined });
    confirm = { confirm: jest.fn().mockResolvedValue(true) };
    toast = { success: jest.fn(), error: jest.fn() };
    dialogRef = { afterClosed: jest.fn().mockReturnValue(of(null)) };
    dialog = { open: jest.fn().mockReturnValue(dialogRef) };

    await TestBed.configureTestingModule({
      imports: [NewsListPage, TranslateModule.forRoot()],
      providers: [
        provideNoopAnimations(),
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: PublishingApiService, useValue: { listNews, publishNews, deleteNews } },
        { provide: ConfirmDialogService, useValue: confirm },
        { provide: ToastService, useValue: toast },
        { provide: MatDialog, useValue: dialog },
        { provide: AuthService, useValue: { currentUser: () => null, isAuthenticated: () => false, hasPermission: () => true } },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(NewsListPage);
    page = fixture.componentInstance;
  });

  it('loads on init', async () => {
    fixture.detectChanges();
    await fixture.whenStable();
    expect(listNews).toHaveBeenCalled();
    expect(page.rows()).toEqual([NEWS]);
  });

  it('publish confirms then POSTs + reloads', async () => {
    fixture.detectChanges();
    await fixture.whenStable();
    listNews.mockClear();
    await page.publish(NEWS);
    expect(confirm.confirm).toHaveBeenCalled();
    expect(publishNews).toHaveBeenCalledWith('n1');
    expect(toast.success).toHaveBeenCalledWith('news.publish.toast');
    expect(listNews).toHaveBeenCalled();
  });

  it('delete confirms then DELETEs + reloads', async () => {
    fixture.detectChanges();
    await fixture.whenStable();
    listNews.mockClear();
    await page.delete(NEWS);
    expect(deleteNews).toHaveBeenCalledWith('n1');
    expect(toast.success).toHaveBeenCalledWith('news.delete.toast');
  });

  it('publishedFilter "" → undefined; "true" → true', async () => {
    fixture.detectChanges();
    await fixture.whenStable();
    listNews.mockClear();
    page.onPublishedFilter('true');
    await Promise.resolve();
    expect(listNews).toHaveBeenLastCalledWith(expect.objectContaining({ isPublished: true }));
  });
});
