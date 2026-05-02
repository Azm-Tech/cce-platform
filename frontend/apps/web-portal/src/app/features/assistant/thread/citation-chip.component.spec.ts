import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { TranslateModule } from '@ngx-translate/core';
import type { Citation } from '../assistant.types';
import { CitationChipComponent } from './citation-chip.component';

const RESOURCE_CITATION: Citation = {
  id: 'r1',
  kind: 'resource',
  title: 'A research paper',
  href: '/knowledge-center/resources/r1',
  sourceText: 'Excerpt about CCE',
};

const MAP_CITATION: Citation = {
  id: 'n1',
  kind: 'map-node',
  title: 'Carbon Capture',
  href: '/knowledge-maps/m1?node=n1',
};

describe('CitationChipComponent', () => {
  let fixture: ComponentFixture<CitationChipComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CitationChipComponent, TranslateModule.forRoot()],
      providers: [provideRouter([]), provideNoopAnimations()],
    }).compileComponents();
    fixture = TestBed.createComponent(CitationChipComponent);
  });

  it('renders [N] index in footer variant', () => {
    fixture.componentRef.setInput('citation', RESOURCE_CITATION);
    fixture.componentRef.setInput('index', 1);
    fixture.componentRef.setInput('variant', 'footer');
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('[1]');
    expect(fixture.nativeElement.textContent).toContain('A research paper');
  });

  it('inline variant shows only the index, no title', () => {
    fixture.componentRef.setInput('citation', RESOURCE_CITATION);
    fixture.componentRef.setInput('index', 2);
    fixture.componentRef.setInput('variant', 'inline');
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('[2]');
    expect(fixture.nativeElement.textContent).not.toContain('A research paper');
  });

  it('uses description icon for resource citations', () => {
    fixture.componentRef.setInput('citation', RESOURCE_CITATION);
    fixture.componentRef.setInput('index', 1);
    fixture.detectChanges();
    expect(fixture.componentInstance.iconName()).toBe('description');
  });

  it('uses account_tree icon for map-node citations', () => {
    fixture.componentRef.setInput('citation', MAP_CITATION);
    fixture.componentRef.setInput('index', 1);
    fixture.detectChanges();
    expect(fixture.componentInstance.iconName()).toBe('account_tree');
  });

  it('tooltip includes title and sourceText when present', () => {
    fixture.componentRef.setInput('citation', RESOURCE_CITATION);
    fixture.componentRef.setInput('index', 1);
    fixture.detectChanges();
    expect(fixture.componentInstance.tooltip()).toContain('A research paper');
    expect(fixture.componentInstance.tooltip()).toContain('Excerpt about CCE');
  });

  it('tooltip is just the title when no sourceText', () => {
    fixture.componentRef.setInput('citation', MAP_CITATION);
    fixture.componentRef.setInput('index', 1);
    fixture.detectChanges();
    expect(fixture.componentInstance.tooltip()).toBe('Carbon Capture');
  });

  it('renders an anchor with the citation href', () => {
    fixture.componentRef.setInput('citation', RESOURCE_CITATION);
    fixture.componentRef.setInput('index', 1);
    fixture.detectChanges();
    const anchor = fixture.nativeElement.querySelector('a') as HTMLAnchorElement;
    expect(anchor.getAttribute('href')).toBe('/knowledge-center/resources/r1');
  });
});
