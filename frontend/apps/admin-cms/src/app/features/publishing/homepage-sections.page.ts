import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSelectModule } from '@angular/material/select';
import { TranslateModule } from '@ngx-translate/core';
import { PermissionDirective } from '../../core/auth/permission.directive';
import { ConfirmDialogService, ToastService } from '@frontend/ui-kit';
import { PublishingApiService } from './publishing-api.service';
import {
  HOMEPAGE_SECTION_TYPES,
  type HomepageSection,
  type HomepageSectionType,
} from './publishing.types';

/**
 * Admin → Homepage sections. Single-page CRUD + reorder. The list is small
 * (≤ ~10 sections) so the UI shows every section as an editable card with
 * up/down arrows for reordering. No drag-and-drop in v0.1.0.
 */
@Component({
  selector: 'cce-homepage-sections',
  standalone: true,
  imports: [
    CommonModule, FormsModule,
    MatButtonModule, MatCardModule, MatCheckboxModule, MatFormFieldModule,
    MatIconModule, MatInputModule, MatProgressBarModule, MatSelectModule,
    TranslateModule, PermissionDirective,
  ],
  templateUrl: './homepage-sections.page.html',
  styleUrl: './homepage-sections.page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class HomepageSectionsPage implements OnInit {
  private readonly api = inject(PublishingApiService);
  private readonly toast = inject(ToastService);
  private readonly confirm = inject(ConfirmDialogService);

  readonly sectionTypes = HOMEPAGE_SECTION_TYPES;
  readonly rows = signal<HomepageSection[]>([]);
  readonly loading = signal(false);
  readonly errorKind = signal<string | null>(null);

  readonly newSectionType = signal<HomepageSectionType>('Hero');
  readonly newContentAr = signal('');
  readonly newContentEn = signal('');

  ngOnInit(): void { void this.load(); }

  async load(): Promise<void> {
    this.loading.set(true);
    this.errorKind.set(null);
    const res = await this.api.listHomepageSections();
    this.loading.set(false);
    if (res.ok) {
      const sorted = [...res.value].sort((a, b) => a.orderIndex - b.orderIndex);
      this.rows.set(sorted);
    } else this.errorKind.set(res.error.kind);
  }

  async create(): Promise<void> {
    const res = await this.api.createHomepageSection({
      sectionType: this.newSectionType(),
      orderIndex: this.rows().length,
      contentAr: this.newContentAr(),
      contentEn: this.newContentEn(),
    });
    if (res.ok) {
      this.toast.success('homepage.create.toast');
      this.newContentAr.set('');
      this.newContentEn.set('');
      void this.load();
    } else this.toast.error(`errors.${res.error.kind}`);
  }

  async update(row: HomepageSection, patch: Partial<{ contentAr: string; contentEn: string; isActive: boolean }>): Promise<void> {
    const res = await this.api.updateHomepageSection(row.id, {
      contentAr: patch.contentAr ?? row.contentAr,
      contentEn: patch.contentEn ?? row.contentEn,
      isActive: patch.isActive ?? row.isActive,
    });
    if (res.ok) {
      this.toast.success('homepage.edit.toast');
      void this.load();
    } else this.toast.error(`errors.${res.error.kind}`);
  }

  async delete(row: HomepageSection): Promise<void> {
    if (!(await this.confirm.confirm({
      titleKey: 'homepage.delete.title', messageKey: 'homepage.delete.message',
      confirmKey: 'homepage.delete.confirm', cancelKey: 'common.actions.cancel',
    }))) return;
    const res = await this.api.deleteHomepageSection(row.id);
    if (res.ok) {
      this.toast.success('homepage.delete.toast');
      void this.load();
    } else this.toast.error(`errors.${res.error.kind}`);
  }

  async moveUp(row: HomepageSection): Promise<void> {
    const idx = this.rows().findIndex((r) => r.id === row.id);
    if (idx <= 0) return;
    await this.swap(idx, idx - 1);
  }

  async moveDown(row: HomepageSection): Promise<void> {
    const idx = this.rows().findIndex((r) => r.id === row.id);
    if (idx < 0 || idx >= this.rows().length - 1) return;
    await this.swap(idx, idx + 1);
  }

  private async swap(a: number, b: number): Promise<void> {
    const items = [...this.rows()];
    const tmp = items[a];
    items[a] = items[b];
    items[b] = tmp;
    const res = await this.api.reorderHomepageSections({
      assignments: items.map((s, i) => ({ id: s.id, orderIndex: i })),
    });
    if (res.ok) {
      this.toast.success('homepage.reorder.toast');
      void this.load();
    } else this.toast.error(`errors.${res.error.kind}`);
  }
}
