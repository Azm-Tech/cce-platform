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
import { ResourceCategoriesPage } from './resource-categories.page';
import { TaxonomyApiService, type Result } from './taxonomy-api.service';
import type { PagedResult, ResourceCategory } from './taxonomy.types';

const CAT: ResourceCategory = {
  id: 'c1', nameAr: 'a', nameEn: 'b', slug: 's', parentId: null, orderIndex: 0, isActive: true,
};

describe('ResourceCategoriesPage', () => {
  let fixture: ComponentFixture<ResourceCategoriesPage>;
  let page: ResourceCategoriesPage;
  let listCategories: jest.Mock;
  let deleteCategory: jest.Mock;
  let confirm: { confirm: jest.Mock };
  let toast: { success: jest.Mock; error: jest.Mock };
  let dialog: { open: jest.Mock };
  let dialogRef: { afterClosed: jest.Mock };

  function ok(value: PagedResult<ResourceCategory>): Result<PagedResult<ResourceCategory>> {
    return { ok: true, value };
  }

  beforeEach(async () => {
    listCategories = jest.fn().mockResolvedValue(ok({ items: [CAT], page: 1, pageSize: 20, total: 1 }));
    deleteCategory = jest.fn().mockResolvedValue({ ok: true, value: undefined });
    confirm = { confirm: jest.fn().mockResolvedValue(true) };
    toast = { success: jest.fn(), error: jest.fn() };
    dialogRef = { afterClosed: jest.fn().mockReturnValue(of(CAT)) };
    dialog = { open: jest.fn().mockReturnValue(dialogRef) };

    await TestBed.configureTestingModule({
      imports: [ResourceCategoriesPage, TranslateModule.forRoot()],
      providers: [
        provideNoopAnimations(),
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: TaxonomyApiService, useValue: { listCategories, deleteCategory } },
        { provide: ConfirmDialogService, useValue: confirm },
        { provide: ToastService, useValue: toast },
        { provide: MatDialog, useValue: dialog },
        { provide: AuthService, useValue: { currentUser: () => null, isAuthenticated: () => false, hasPermission: () => true } },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(ResourceCategoriesPage);
    page = fixture.componentInstance;
  });

  it('loads on init', async () => {
    fixture.detectChanges();
    await fixture.whenStable();
    expect(listCategories).toHaveBeenCalled();
    expect(page.rows()).toEqual([CAT]);
  });

  it('openCreate opens dialog + reloads on success', async () => {
    fixture.detectChanges();
    await fixture.whenStable();
    listCategories.mockClear();
    await page.openCreate();
    expect(dialog.open).toHaveBeenCalled();
    expect(toast.success).toHaveBeenCalledWith('taxonomies.category.create.toast');
    expect(listCategories).toHaveBeenCalled();
  });

  it('delete confirms then DELETEs', async () => {
    fixture.detectChanges();
    await fixture.whenStable();
    await page.delete(CAT);
    expect(deleteCategory).toHaveBeenCalledWith('c1');
    expect(toast.success).toHaveBeenCalledWith('taxonomies.category.delete.toast');
  });
});
