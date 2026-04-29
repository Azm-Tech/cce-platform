import { Component } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { TranslateModule } from '@ngx-translate/core';
import { PagedTableColumn, PagedTableComponent, PagedTablePageChange } from './paged-table.component';

interface Row {
  id: string;
  name: string;
  email: string;
}

const ROWS: Row[] = [
  { id: '1', name: 'Alice', email: 'alice@example.com' },
  { id: '2', name: 'Bob', email: 'bob@example.com' },
];

const COLUMNS: PagedTableColumn<Row>[] = [
  { key: 'name', labelKey: 'col.name', cell: (r) => r.name },
  { key: 'email', labelKey: 'col.email', cell: (r) => r.email },
];

@Component({
  standalone: true,
  imports: [PagedTableComponent],
  template: `
    <cce-paged-table
      [columns]="columns"
      [rows]="rows"
      [total]="total"
      [page]="page"
      [pageSize]="pageSize"
      [loading]="loading"
      (pageChange)="onPage($event)"
    />
  `,
})
class HostComponent {
  columns = COLUMNS;
  rows: Row[] = ROWS;
  total = 42;
  page = 1;
  pageSize = 20;
  loading = false;
  lastPage: PagedTablePageChange | null = null;
  onPage(e: PagedTablePageChange): void {
    this.lastPage = e;
  }
}

describe('PagedTableComponent', () => {
  let fixture: ComponentFixture<HostComponent>;
  let host: HostComponent;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [HostComponent, TranslateModule.forRoot()],
      providers: [provideNoopAnimations()],
    }).compileComponents();

    fixture = TestBed.createComponent(HostComponent);
    host = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('renders one header cell per column', () => {
    const headers = fixture.nativeElement.querySelectorAll('th[mat-header-cell]');
    expect(headers).toHaveLength(2);
  });

  it('renders one data row per row input', () => {
    const rows = fixture.nativeElement.querySelectorAll('tr[mat-row]');
    expect(rows).toHaveLength(2);
  });

  it('renders cell values via the column cell() function', () => {
    const cells = fixture.nativeElement.querySelectorAll('td[mat-cell]');
    expect(cells[0].textContent.trim()).toBe('Alice');
    expect(cells[1].textContent.trim()).toBe('alice@example.com');
    expect(cells[2].textContent.trim()).toBe('Bob');
    expect(cells[3].textContent.trim()).toBe('bob@example.com');
  });

  it('shows the progress bar only when loading is true', () => {
    expect(fixture.nativeElement.querySelector('mat-progress-bar')).toBeNull();
    host.loading = true;
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('mat-progress-bar')).not.toBeNull();
  });

  it('binds total + pageIndex + pageSize to mat-paginator', () => {
    const paginator = fixture.nativeElement.querySelector('mat-paginator');
    expect(paginator).not.toBeNull();
    // length is the rendered total count attr in Material 18; check the input via component instance.
    const tableEl = fixture.debugElement.query((d) => d.componentInstance instanceof PagedTableComponent);
    const tableInstance = tableEl.componentInstance as PagedTableComponent<Row>;
    expect(tableInstance.total).toBe(42);
    expect(tableInstance.pageIndex).toBe(0); // page 1 → pageIndex 0
    expect(tableInstance.pageSize).toBe(20);
  });

  it('converts 1-based page input to 0-based pageIndex', () => {
    host.page = 3;
    fixture.detectChanges();
    const tableEl = fixture.debugElement.query((d) => d.componentInstance instanceof PagedTableComponent);
    const tableInstance = tableEl.componentInstance as PagedTableComponent<Row>;
    expect(tableInstance.pageIndex).toBe(2);
  });

  it('emits 1-based pageChange when paginator emits', () => {
    const tableEl = fixture.debugElement.query((d) => d.componentInstance instanceof PagedTableComponent);
    const tableInstance = tableEl.componentInstance as PagedTableComponent<Row>;
    tableInstance.onPage({ pageIndex: 2, pageSize: 50, length: 42, previousPageIndex: 1 });
    expect(host.lastPage).toEqual({ page: 3, pageSize: 50 });
  });

  it('exposes displayedColumns derived from columns input', () => {
    const tableEl = fixture.debugElement.query((d) => d.componentInstance instanceof PagedTableComponent);
    const tableInstance = tableEl.componentInstance as PagedTableComponent<Row>;
    expect(tableInstance.displayedColumns).toEqual(['name', 'email']);
  });
});
