import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { MatDialog } from '@angular/material/dialog';
import { TranslateModule } from '@ngx-translate/core';
import { of } from 'rxjs';
import { AuthService } from '../../core/auth/auth.service';
import { ConfirmDialogService } from '../../core/ui/confirm-dialog.service';
import { ToastService } from '../../core/ui/toast.service';
import { TaxonomyApiService, type Result } from './taxonomy-api.service';
import type { PagedResult, Topic } from './taxonomy.types';
import { TopicsPage } from './topics.page';

const T: Topic = {
  id: 't1', nameAr: 'a', nameEn: 'b', descriptionAr: '', descriptionEn: '',
  slug: 's', parentId: null, iconUrl: null, orderIndex: 0, isActive: true,
};

describe('TopicsPage', () => {
  let fixture: ComponentFixture<TopicsPage>;
  let page: TopicsPage;
  let listTopics: jest.Mock;
  let deleteTopic: jest.Mock;
  let confirm: { confirm: jest.Mock };
  let toast: { success: jest.Mock; error: jest.Mock };
  let dialog: { open: jest.Mock };
  let dialogRef: { afterClosed: jest.Mock };

  function ok(value: PagedResult<Topic>): Result<PagedResult<Topic>> {
    return { ok: true, value };
  }

  beforeEach(async () => {
    listTopics = jest.fn().mockResolvedValue(ok({ items: [T], page: 1, pageSize: 20, total: 1 }));
    deleteTopic = jest.fn().mockResolvedValue({ ok: true, value: undefined });
    confirm = { confirm: jest.fn().mockResolvedValue(true) };
    toast = { success: jest.fn(), error: jest.fn() };
    dialogRef = { afterClosed: jest.fn().mockReturnValue(of(T)) };
    dialog = { open: jest.fn().mockReturnValue(dialogRef) };

    await TestBed.configureTestingModule({
      imports: [TopicsPage, TranslateModule.forRoot()],
      providers: [
        provideNoopAnimations(),
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: TaxonomyApiService, useValue: { listTopics, deleteTopic } },
        { provide: ConfirmDialogService, useValue: confirm },
        { provide: ToastService, useValue: toast },
        { provide: MatDialog, useValue: dialog },
        { provide: AuthService, useValue: { currentUser: () => null, isAuthenticated: () => false, hasPermission: () => true } },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(TopicsPage);
    page = fixture.componentInstance;
  });

  it('loads on init with no search', async () => {
    fixture.detectChanges();
    await fixture.whenStable();
    expect(listTopics).toHaveBeenCalledWith({ page: 1, pageSize: 20, search: undefined });
  });

  it('onSearch resets page + passes search query', async () => {
    page.searchInput.set('q');
    page.page.set(3);
    listTopics.mockClear();
    page.onSearch();
    await Promise.resolve();
    expect(page.page()).toBe(1);
    expect(listTopics).toHaveBeenCalledWith({ page: 1, pageSize: 20, search: 'q' });
  });

  it('delete confirms then DELETEs', async () => {
    fixture.detectChanges();
    await fixture.whenStable();
    await page.delete(T);
    expect(deleteTopic).toHaveBeenCalledWith('t1');
    expect(toast.success).toHaveBeenCalledWith('taxonomies.topic.delete.toast');
  });
});
