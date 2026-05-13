import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatChipsModule } from '@angular/material/chips';
import { TranslateModule } from '@ngx-translate/core';
import { MOCK_TRANSLATIONS, type TranslationEntry } from './translations-mock';

type Scope = 'all' | TranslationEntry['scope'];

/**
 * Translations management page (BRD §4.1.24).
 *
 * Side-by-side English / Arabic editor for the i18n catalogue. Lets
 * an admin update strings without code changes. The current version
 * is demo-mode: it operates against a frozen client-side copy of the
 * catalogue (`MOCK_TRANSLATIONS`). When the backend ships
 * `/api/admin/translations` (GET + PATCH), wire it through here.
 *
 * UX:
 *   • Search across keys, English, and Arabic content.
 *   • Filter by scope chip (Common / Auth / Nav / Home / Resources / Events / Errors).
 *   • Edit-in-place with keyboard-friendly inputs; row gets a "modified"
 *     indicator. Save All commits all dirty rows in one batch (mock).
 *   • Counter chips: total, modified, scopes.
 */
@Component({
  selector: 'cce-translations',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatButtonModule,
    MatChipsModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatSelectModule,
    TranslateModule,
  ],
  templateUrl: './translations.page.html',
  styleUrl: './translations.page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TranslationsPage {
  readonly scopes: ReadonlyArray<{ id: Scope; label: string }> = [
    { id: 'all',       label: 'All' },
    { id: 'common',    label: 'Common' },
    { id: 'auth',      label: 'Auth' },
    { id: 'nav',       label: 'Nav' },
    { id: 'home',      label: 'Home' },
    { id: 'resources', label: 'Resources' },
    { id: 'events',    label: 'Events' },
    { id: 'errors',    label: 'Errors' },
  ];

  readonly searchTerm = signal('');
  readonly selectedScope = signal<Scope>('all');
  readonly entries = signal<TranslationEntry[]>(structuredClone(MOCK_TRANSLATIONS));
  readonly originalEntries = signal<TranslationEntry[]>(structuredClone(MOCK_TRANSLATIONS));

  readonly filteredEntries = computed<TranslationEntry[]>(() => {
    const term = this.searchTerm().trim().toLowerCase();
    const scope = this.selectedScope();
    return this.entries().filter((e) => {
      if (scope !== 'all' && e.scope !== scope) return false;
      if (!term) return true;
      return (
        e.key.toLowerCase().includes(term) ||
        e.en.toLowerCase().includes(term) ||
        e.ar.toLowerCase().includes(term)
      );
    });
  });

  readonly modifiedCount = computed(() => {
    const orig = this.originalEntries();
    let n = 0;
    for (const e of this.entries()) {
      const o = orig.find((x) => x.key === e.key);
      if (o && (o.en !== e.en || o.ar !== e.ar)) n++;
    }
    return n;
  });

  readonly stats = computed(() => {
    const all = this.entries();
    const scopeSet = new Set(all.map((e) => e.scope));
    return {
      total: all.length,
      scopes: scopeSet.size,
    };
  });

  constructor(private readonly snack: MatSnackBar) {}

  isDirty(key: string): boolean {
    const orig = this.originalEntries().find((e) => e.key === key);
    const cur = this.entries().find((e) => e.key === key);
    if (!orig || !cur) return false;
    return orig.en !== cur.en || orig.ar !== cur.ar;
  }

  updateField(key: string, lang: 'en' | 'ar', value: string): void {
    this.entries.update((arr) =>
      arr.map((e) => (e.key === key ? { ...e, [lang]: value } : e)),
    );
  }

  saveAll(): void {
    if (this.modifiedCount() === 0) return;
    // Demo: snapshot the current edited state as the new baseline + toast.
    // Real prod would PATCH /api/admin/translations with the diff.
    const today = new Date().toISOString().slice(0, 10);
    this.entries.update((arr) =>
      arr.map((e) =>
        this.isDirty(e.key) ? { ...e, updatedAt: today } : e,
      ),
    );
    this.originalEntries.set(structuredClone(this.entries()));
    this.snack.open(`Saved ${this.modifiedCount()} translation(s).`, 'Dismiss', { duration: 3000 });
  }

  resetAll(): void {
    this.entries.set(structuredClone(this.originalEntries()));
    this.snack.open('Reverted unsaved changes.', 'Dismiss', { duration: 2000 });
  }

  clearFilters(): void {
    this.searchTerm.set('');
    this.selectedScope.set('all');
  }

  trackByKey(_idx: number, entry: TranslationEntry): string {
    return entry.key;
  }
}
