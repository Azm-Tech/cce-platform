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
import { ContentApiService, type Result } from './content-api.service';
import type { PagedResult, Resource } from './content.types';
import { ResourcesListPage } from './resources-list.page';

const RESOURCE: Resource = {
  id: 'r1',
  titleAr: 't-ar',
  titleEn: 't-en',
  descriptionAr: 'd-ar',
  descriptionEn: 'd-en',
  resourceType: 'Pdf',
  categoryId: 'cat1',
  countryId: null,
  uploadedById: 'admin',
  assetFileId: 'asset1',
  publishedOn: null,
  viewCount: 0,
  isCenterManaged: true,
  isPublished: false,
  rowVersion: 'AAAA=',
};

describe('ResourcesListPage', () => {
  let fixture: ComponentFixture<ResourcesListPage>;
  let page: ResourcesListPage;
  let listResources: jest.Mock;
  let publishResource: jest.Mock;
  let dialog: { open: jest.Mock };
  let dialogRef: { afterClosed: jest.Mock };
  let confirm: { confirm: jest.Mock };
  let toast: { success: jest.Mock; error: jest.Mock };

  function ok(value: PagedResult<Resource>): Result<PagedResult<Resource>> {
    return { ok: true, value };
  }

  beforeEach(async () => {
    listResources = jest.fn().mockResolvedValue(ok({ items: [RESOURCE], page: 1, pageSize: 20, total: 1 }));
    publishResource = jest.fn().mockResolvedValue({ ok: true, value: { ...RESOURCE, isPublished: true } });
    dialogRef = { afterClosed: jest.fn().mockReturnValue(of(null)) };
    dialog = { open: jest.fn().mockReturnValue(dialogRef) };
    confirm = { confirm: jest.fn().mockResolvedValue(true) };
    toast = { success: jest.fn(), error: jest.fn() };

    await TestBed.configureTestingModule({
      imports: [ResourcesListPage, TranslateModule.forRoot()],
      providers: [
        provideNoopAnimations(),
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: ContentApiService, useValue: { listResources, publishResource } },
        { provide: MatDialog, useValue: dialog },
        { provide: ConfirmDialogService, useValue: confirm },
        { provide: ToastService, useValue: toast },
        { provide: AuthService, useValue: { currentUser: () => null, isAuthenticated: () => false, hasPermission: () => true } },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(ResourcesListPage);
    page = fixture.componentInstance;
  });

  it('loads on init with no published filter', async () => {
    fixture.detectChanges();
    await fixture.whenStable();
    expect(listResources).toHaveBeenCalledWith({
      page: 1,
      pageSize: 20,
      search: undefined,
      isPublished: undefined,
    });
  });

  it('publishedFilter true → isPublished true; false → false; "" → undefined', async () => {
    fixture.detectChanges();
    await fixture.whenStable();
    listResources.mockClear();
    page.onPublishedFilter('true');
    await Promise.resolve();
    expect(listResources).toHaveBeenLastCalledWith(expect.objectContaining({ isPublished: true }));
    page.onPublishedFilter('false');
    await Promise.resolve();
    expect(listResources).toHaveBeenLastCalledWith(expect.objectContaining({ isPublished: false }));
    page.onPublishedFilter('');
    await Promise.resolve();
    expect(listResources).toHaveBeenLastCalledWith(expect.objectContaining({ isPublished: undefined }));
  });

  it('openCreate opens dialog; reload + toast on success', async () => {
    fixture.detectChanges();
    await fixture.whenStable();
    dialogRef.afterClosed.mockReturnValue(of(RESOURCE));
    listResources.mockClear();
    await page.openCreate();
    expect(dialog.open).toHaveBeenCalled();
    expect(toast.success).toHaveBeenCalledWith('resources.create.toast');
    expect(listResources).toHaveBeenCalled();
  });

  it('openEdit opens dialog with resource data; toast on success', async () => {
    fixture.detectChanges();
    await fixture.whenStable();
    dialogRef.afterClosed.mockReturnValue(of(RESOURCE));
    await page.openEdit(RESOURCE);
    expect(dialog.open).toHaveBeenCalledWith(expect.anything(), expect.objectContaining({
      data: { resource: RESOURCE },
    }));
    expect(toast.success).toHaveBeenCalledWith('resources.edit.toast');
  });

  it('publish confirms then POSTs + reloads', async () => {
    fixture.detectChanges();
    await fixture.whenStable();
    listResources.mockClear();
    await page.publish(RESOURCE);
    expect(confirm.confirm).toHaveBeenCalled();
    expect(publishResource).toHaveBeenCalledWith('r1');
    expect(toast.success).toHaveBeenCalledWith('resources.publish.toast');
    expect(listResources).toHaveBeenCalled();
  });

  it('publish skips when confirm cancelled', async () => {
    fixture.detectChanges();
    await fixture.whenStable();
    confirm.confirm.mockResolvedValueOnce(false);
    await page.publish(RESOURCE);
    expect(publishResource).not.toHaveBeenCalled();
  });

  it('publish surfaces api error via toast.error', async () => {
    fixture.detectChanges();
    await fixture.whenStable();
    publishResource.mockResolvedValueOnce({ ok: false, error: { kind: 'forbidden' } });
    await page.publish(RESOURCE);
    expect(toast.error).toHaveBeenCalledWith('errors.forbidden');
  });
});
