import { ComponentFixture, TestBed } from '@angular/core/testing';
import { AppShellComponent } from './app-shell.component';

describe('AppShellComponent', () => {
  let fixture: ComponentFixture<AppShellComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({ imports: [AppShellComponent] }).compileComponents();
    fixture = TestBed.createComponent(AppShellComponent);
    fixture.componentRef.setInput('appTitle', 'Test App');
    fixture.detectChanges();
  });

  it('renders the appTitle input in the toolbar', () => {
    const toolbar = fixture.nativeElement.querySelector('mat-toolbar');
    expect(toolbar?.textContent).toContain('Test App');
  });

  it('exposes a content projection slot for main content', () => {
    expect(fixture.nativeElement.querySelector('main')).not.toBeNull();
  });
});
