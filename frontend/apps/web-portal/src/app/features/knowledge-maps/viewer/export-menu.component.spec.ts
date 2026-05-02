import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { TranslateModule } from '@ngx-translate/core';
import { ExportMenuComponent, type ExportFormat } from './export-menu.component';

describe('ExportMenuComponent', () => {
  let fixture: ComponentFixture<ExportMenuComponent>;
  let component: ExportMenuComponent;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ExportMenuComponent, TranslateModule.forRoot()],
      providers: [provideNoopAnimations()],
    }).compileComponents();
    fixture = TestBed.createComponent(ExportMenuComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('renders a stroked-button trigger labelled "Export"', () => {
    const html = fixture.nativeElement.textContent ?? '';
    expect(html).toContain('knowledgeMaps.export.title');
  });

  it('exposes 4 export formats in the public formats array', () => {
    expect(component.formats).toEqual(['png', 'svg', 'json', 'pdf']);
  });

  it('onChoose(format) emits (formatChosen) with the right format when not disabled', () => {
    const captured: ExportFormat[] = [];
    component.formatChosen.subscribe((f) => captured.push(f));
    component.onChoose('png');
    component.onChoose('json');
    expect(captured).toEqual(['png', 'json']);
  });

  it('onChoose(format) is a no-op when disabled', () => {
    fixture.componentRef.setInput('disabled', true);
    fixture.detectChanges();
    let emitted: ExportFormat | null = null;
    component.formatChosen.subscribe((f) => { emitted = f; });
    component.onChoose('png');
    expect(emitted).toBeNull();
  });
});
