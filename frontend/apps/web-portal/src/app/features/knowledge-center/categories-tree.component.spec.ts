import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { TranslateModule } from '@ngx-translate/core';
import { CategoriesTreeComponent } from './categories-tree.component';
import type { ResourceCategory } from './knowledge.types';

describe('CategoriesTreeComponent', () => {
  let fixture: ComponentFixture<CategoriesTreeComponent>;
  let component: CategoriesTreeComponent;

  const fixtures = {
    rootB: { id: 'b', nameAr: 'ب', nameEn: 'B', slug: 'b', parentId: null, orderIndex: 1 } satisfies ResourceCategory,
    rootA: { id: 'a', nameAr: 'أ', nameEn: 'A', slug: 'a', parentId: null, orderIndex: 0 } satisfies ResourceCategory,
    childOfA: { id: 'a1', nameAr: 'أ١', nameEn: 'A1', slug: 'a1', parentId: 'a', orderIndex: 0 } satisfies ResourceCategory,
  };

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CategoriesTreeComponent, TranslateModule.forRoot()],
      providers: [provideNoopAnimations()],
    }).compileComponents();
    fixture = TestBed.createComponent(CategoriesTreeComponent);
    component = fixture.componentInstance;
  });

  it('renders root categories sorted by orderIndex', () => {
    component.categories = [fixtures.rootB, fixtures.rootA];
    fixture.detectChanges();
    const buttons = Array.from(
      fixture.nativeElement.querySelectorAll('.cce-cat-tree__list > .cce-cat-tree__node > .cce-cat-tree__item'),
    ) as HTMLButtonElement[];
    expect(buttons.map((b) => b.textContent?.trim())).toEqual(['A', 'B']);
  });

  it('renders nested children under their parent', () => {
    component.categories = [fixtures.rootA, fixtures.childOfA];
    fixture.detectChanges();
    const nestedList = fixture.nativeElement.querySelector('.cce-cat-tree__list--nested');
    expect(nestedList).not.toBeNull();
    const childButton = nestedList.querySelector('.cce-cat-tree__item--child');
    expect(childButton.textContent.trim()).toBe('A1');
  });

  it('emits selectionChange on click; All button emits null', () => {
    component.categories = [fixtures.rootA];
    fixture.detectChanges();
    const emitted: (string | null)[] = [];
    component.selectionChange.subscribe((v) => emitted.push(v));

    const allBtn = fixture.nativeElement.querySelector('.cce-cat-tree__item--all') as HTMLButtonElement;
    const aBtn = Array.from<HTMLButtonElement>(
      fixture.nativeElement.querySelectorAll('.cce-cat-tree__list > .cce-cat-tree__node > .cce-cat-tree__item'),
    ).find((b) => b.textContent?.trim() === 'A') as HTMLButtonElement;

    aBtn.click();
    allBtn.click();

    expect(emitted).toEqual(['a', null]);
  });
});
