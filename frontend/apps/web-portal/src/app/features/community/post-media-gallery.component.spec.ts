import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { TranslocoTestingModule } from '@jsverse/transloco';
import type { PostMedia } from './community.types';
import { PostMediaGalleryComponent } from './post-media-gallery.component';

const IMG: PostMedia = {
  assetFileId: 'img1', kind: 'media', mimeType: 'image/png', url: 'https://cdn/x/photo.png',
  sizeBytes: 2048, originalFileName: 'photo.png', sortOrder: 0,
};
const VID: PostMedia = {
  assetFileId: 'vid1', kind: 'media', mimeType: 'video/mp4', url: 'https://cdn/x/clip.mp4',
  sizeBytes: 4096, originalFileName: 'clip.mp4', sortOrder: 1,
};
const DOC: PostMedia = {
  assetFileId: 'doc1', kind: 'document', mimeType: 'application/pdf', url: 'https://cdn/x/report.pdf',
  sizeBytes: 8192, originalFileName: 'report.pdf', sortOrder: 2,
};

describe('PostMediaGalleryComponent', () => {
  let fixture: ComponentFixture<PostMediaGalleryComponent>;

  function setMedia(media: PostMedia[]): void {
    fixture.componentRef.setInput('media', media);
    fixture.detectChanges();
  }

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [
        PostMediaGalleryComponent,
        TranslocoTestingModule.forRoot({
          langs: { en: {}, ar: {} },
          translocoConfig: { availableLangs: ['en', 'ar'], defaultLang: 'en' },
        }),
      ],
      providers: [provideNoopAnimations()],
    }).compileComponents();

    fixture = TestBed.createComponent(PostMediaGalleryComponent);
  });

  it('renders image, video, and file by category', () => {
    setMedia([IMG, VID, DOC]);
    const el = fixture.nativeElement as HTMLElement;
    expect(el.querySelector('.cce-gallery__image img')).not.toBeNull();
    expect(el.querySelector('.cce-gallery__video video')).not.toBeNull();
    expect(el.querySelector('.cce-gallery__file')).not.toBeNull();
  });

  it('skips items without a url', () => {
    setMedia([{ ...IMG, url: '' }]);
    expect((fixture.nativeElement as HTMLElement).querySelector('.cce-gallery__image')).toBeNull();
  });

  it('opens the lightbox when an image is clicked', () => {
    setMedia([IMG, DOC]);
    const imgBtn = fixture.nativeElement.querySelector('.cce-gallery__image') as HTMLButtonElement;
    expect(imgBtn).not.toBeNull();
    imgBtn.click();
    fixture.detectChanges();
    expect(fixture.componentInstance.lightboxIndex()).toBe(0);
    expect(fixture.nativeElement.querySelector('cce-media-lightbox')).not.toBeNull();
  });
});
