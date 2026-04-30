import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { TranslateModule } from '@ngx-translate/core';
import { AssetUploadComponent } from './asset-upload.component';
import { ContentApiService, type Result } from './content-api.service';
import type { AssetFile } from './content.types';

const ASSET: AssetFile = {
  id: 'asset1',
  url: 'https://cdn.example.com/a.pdf',
  originalFileName: 'a.pdf',
  sizeBytes: 1024,
  mimeType: 'application/pdf',
  uploadedById: 'admin',
  uploadedOn: '2026-04-29',
  virusScanStatus: 'Clean',
  scannedOn: '2026-04-29',
};

describe('AssetUploadComponent', () => {
  let fixture: ComponentFixture<AssetUploadComponent>;
  let component: AssetUploadComponent;
  let uploadAsset: jest.Mock;

  function ok(value: AssetFile): Result<AssetFile> {
    return { ok: true, value };
  }

  function makeFile(): File {
    return new File(['hello'], 'a.pdf', { type: 'application/pdf' });
  }

  function makeChangeEvent(file: File | null): Event {
    const input = document.createElement('input');
    input.type = 'file';
    Object.defineProperty(input, 'files', {
      value: file ? [file] : [],
      configurable: true,
    });
    return { target: input, currentTarget: input } as unknown as Event;
  }

  beforeEach(async () => {
    uploadAsset = jest.fn();
    await TestBed.configureTestingModule({
      imports: [AssetUploadComponent, TranslateModule.forRoot()],
      providers: [
        provideNoopAnimations(),
        { provide: ContentApiService, useValue: { uploadAsset } },
      ],
    }).compileComponents();
    fixture = TestBed.createComponent(AssetUploadComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('does nothing when no file is selected', async () => {
    await component.onFile(makeChangeEvent(null));
    expect(uploadAsset).not.toHaveBeenCalled();
  });

  it('uploads, exposes asset, and emits uploaded on success', async () => {
    uploadAsset.mockResolvedValueOnce(ok(ASSET));
    const emitted: AssetFile[] = [];
    component.uploaded.subscribe((a) => emitted.push(a));
    await component.onFile(makeChangeEvent(makeFile()));
    expect(uploadAsset).toHaveBeenCalled();
    expect(component.asset()).toEqual(ASSET);
    expect(emitted).toEqual([ASSET]);
    expect(component.uploading()).toBe(false);
  });

  it('surfaces errorKind when the upload fails', async () => {
    uploadAsset.mockResolvedValueOnce({ ok: false, error: { kind: 'server' } });
    await component.onFile(makeChangeEvent(makeFile()));
    expect(component.errorKind()).toBe('server');
    expect(component.asset()).toBeNull();
  });

  it('clear() resets asset + errorKind', async () => {
    uploadAsset.mockResolvedValueOnce(ok(ASSET));
    await component.onFile(makeChangeEvent(makeFile()));
    component.clear();
    expect(component.asset()).toBeNull();
  });
});
