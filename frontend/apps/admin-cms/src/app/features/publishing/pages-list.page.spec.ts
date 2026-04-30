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
import type { PagedResult, Page } from './publishing.types';
import { PagesListPage } from './pages-list.page';

const PAGE: Page = {
  id: 'p1',
  slug: 'about',
  pageType: 'AboutPlatform',
  titleAr: 't-ar', titleEn: 't-en',
  contentAr: 'c-ar', contentEn: 'c-en',
  rowVersion: 'v',
};

describe('PagesListPage', () => {
  let fixture: ComponentFixture<PagesListPage>;
  let page: PagesListPage;
  let listPages: jest.Mock;
  let deletePage: jest.Mock;
  let confirm: { confirm: jest.Mock };
  let toast: { success: jest.Mock; error: jest.Mock };
  let dialog: { open: jest.Mock };
  let dialogRef: { afterClosed: jest.Mock };

  function ok(value: PagedResult<Page>): Result<PagedResult<Page>> {
    return { ok: true, value };
  }

  beforeEach(async () => {
    listPages = jest.fn().mockResolvedValue(ok({ items: [PAGE], page: 1, pageSize: 20, total: 1 }));
    deletePage = jest.fn().mockResolvedValue({ ok: true, value: undefined });
    confirm = { confirm: jest.fn().mockResolvedValue(true) };
    toast = { success: jest.fn(), error: jest.fn() };
    dialogRef = { afterClosed: jest.fn().mockReturnValue(of(PAGE)) };
    dialog = { open: jest.fn().mockReturnValue(dialogRef) };

    await TestBed.configureTestingModule({
      imports: [PagesListPage, TranslateModule.forRoot()],
      providers: [
        provideNoopAnimations(),
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: PublishingApiService, useValue: { listPages, deletePage } },
        { provide: ConfirmDialogService, useValue: confirm },
        { provide: ToastService, useValue: toast },
        { provide: MatDialog, useValue: dialog },
        { provide: AuthService, useValue: { currentUser: () => null, isAuthenticated: () => false, hasPermission: () => true } },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(PagesListPage);
    page = fixture.componentInstance;
  });

  it('loads on init', async () => {
    fixture.detectChanges();
    await fixture.whenStable();
    expect(listPages).toHaveBeenCalled();
    expect(page.rows()).toEqual([PAGE]);
  });

  it('openCreate opens dialog; toast on success', async () => {
    fixture.detectChanges();
    await fixture.whenStable();
    await page.openCreate();
    expect(dialog.open).toHaveBeenCalled();
    expect(toast.success).toHaveBeenCalledWith('pages.create.toast');
  });

  it('delete confirms then DELETEs', async () => {
    fixture.detectChanges();
    await fixture.whenStable();
    await page.delete(PAGE);
    expect(deletePage).toHaveBeenCalledWith('p1');
    expect(toast.success).toHaveBeenCalledWith('pages.delete.toast');
  });
});
