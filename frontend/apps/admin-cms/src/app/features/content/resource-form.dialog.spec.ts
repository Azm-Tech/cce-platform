import { TestBed } from '@angular/core/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { TranslateModule } from '@ngx-translate/core';
import { ContentApiService } from './content-api.service';
import {
  ResourceFormDialogComponent,
  type ResourceFormDialogData,
} from './resource-form.dialog';
import type { AssetFile, Resource } from './content.types';

const VALID_GUID = '11111111-1111-1111-1111-111111111111';

const ASSET: AssetFile = {
  id: 'asset1',
  url: '',
  originalFileName: 'a.pdf',
  sizeBytes: 1,
  mimeType: 'application/pdf',
  uploadedById: 'admin',
  uploadedOn: '2026-04-29',
  virusScanStatus: 'Clean',
  scannedOn: null,
};

const RESOURCE: Resource = {
  id: 'r1',
  titleAr: 't-ar',
  titleEn: 't-en',
  descriptionAr: 'd-ar',
  descriptionEn: 'd-en',
  resourceType: 'Pdf',
  categoryId: VALID_GUID,
  countryId: null,
  uploadedById: 'admin',
  assetFileId: 'asset1',
  publishedOn: null,
  viewCount: 0,
  isCenterManaged: true,
  isPublished: false,
  rowVersion: 'AAAA=',
};

function configure(data: ResourceFormDialogData) {
  const createResource = jest.fn();
  const updateResource = jest.fn();
  const dialogRef = { close: jest.fn() };
  TestBed.configureTestingModule({
    imports: [ResourceFormDialogComponent, TranslateModule.forRoot()],
    providers: [
      provideNoopAnimations(),
      { provide: ContentApiService, useValue: { createResource, updateResource, uploadAsset: jest.fn() } },
      { provide: MatDialogRef, useValue: dialogRef },
      { provide: MAT_DIALOG_DATA, useValue: data },
    ],
  });
  const fixture = TestBed.createComponent(ResourceFormDialogComponent);
  const dialog = fixture.componentInstance;
  fixture.detectChanges();
  return { dialog, createResource, updateResource, dialogRef, fixture };
}

describe('ResourceFormDialogComponent', () => {
  describe('create mode', () => {
    it('starts in create mode with isEdit=false', () => {
      const { dialog } = configure({});
      expect(dialog.isEdit).toBe(false);
    });

    it('save() does nothing when form invalid', async () => {
      const { dialog, createResource } = configure({});
      await dialog.save();
      expect(createResource).not.toHaveBeenCalled();
    });

    it('save() requires asset upload before submitting', async () => {
      const { dialog, createResource } = configure({});
      dialog.form.patchValue({
        titleAr: 'a', titleEn: 'b', descriptionAr: 'c', descriptionEn: 'd',
        resourceType: 'Pdf', categoryId: VALID_GUID,
      });
      await dialog.save();
      expect(createResource).not.toHaveBeenCalled();
      expect(dialog.errorKind()).toBe('validation');
    });

    it('save() POSTs createResource with assetFileId on success', async () => {
      const { dialog, createResource, dialogRef } = configure({});
      dialog.form.patchValue({
        titleAr: 'a', titleEn: 'b', descriptionAr: 'c', descriptionEn: 'd',
        resourceType: 'Pdf', categoryId: VALID_GUID, countryId: '',
      });
      dialog.onAssetUploaded(ASSET);
      createResource.mockResolvedValueOnce({ ok: true, value: RESOURCE });
      await dialog.save();
      expect(createResource).toHaveBeenCalledWith({
        titleAr: 'a',
        titleEn: 'b',
        descriptionAr: 'c',
        descriptionEn: 'd',
        resourceType: 'Pdf',
        categoryId: VALID_GUID,
        countryId: null,
        assetFileId: ASSET.id,
      });
      expect(dialogRef.close).toHaveBeenCalledWith(RESOURCE);
    });
  });

  describe('edit mode', () => {
    it('seeds form from data.resource and isEdit=true', () => {
      const { dialog } = configure({ resource: RESOURCE });
      expect(dialog.isEdit).toBe(true);
      expect(dialog.form.controls.titleAr.value).toBe('t-ar');
      expect(dialog.form.controls.categoryId.value).toBe(VALID_GUID);
    });

    it('save() PUTs updateResource with rowVersion', async () => {
      const { dialog, updateResource, dialogRef } = configure({ resource: RESOURCE });
      dialog.form.patchValue({ titleAr: 'new-ar', titleEn: 'new-en' });
      const updated = { ...RESOURCE, titleAr: 'new-ar', titleEn: 'new-en' };
      updateResource.mockResolvedValueOnce({ ok: true, value: updated });
      await dialog.save();
      expect(updateResource).toHaveBeenCalledWith('r1', expect.objectContaining({
        titleAr: 'new-ar',
        titleEn: 'new-en',
        rowVersion: 'AAAA=',
      }));
      expect(dialogRef.close).toHaveBeenCalledWith(updated);
    });

    it('save() surfaces concurrency errorKind on 409', async () => {
      const { dialog, updateResource, dialogRef } = configure({ resource: RESOURCE });
      updateResource.mockResolvedValueOnce({ ok: false, error: { kind: 'concurrency' } });
      await dialog.save();
      expect(dialog.errorKind()).toBe('concurrency');
      expect(dialogRef.close).not.toHaveBeenCalled();
    });
  });

  it('cancel() closes with null', () => {
    const { dialog, dialogRef } = configure({});
    dialog.cancel();
    expect(dialogRef.close).toHaveBeenCalledWith(null);
  });
});
