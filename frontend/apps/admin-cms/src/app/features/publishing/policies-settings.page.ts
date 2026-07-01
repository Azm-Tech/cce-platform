import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatTableModule } from '@angular/material/table';
import { TranslocoModule } from '@jsverse/transloco';
import { ConfirmDialogService, ToastService, TranslateFieldComponent } from '@frontend/ui-kit';
import { PublishingApiService } from './publishing-api.service';
import type { PolicySection } from './publishing.types';

interface SectionForm {
  titleAr: FormControl<string>;
  titleEn: FormControl<string>;
  contentAr: FormControl<string>;
  contentEn: FormControl<string>;
}

@Component({
  selector: 'cce-policies-settings',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatButtonModule,
    MatCardModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressBarModule,
    MatTableModule,
    TranslocoModule,
    TranslateFieldComponent,
  ],
  templateUrl: './policies-settings.page.html',
  styleUrl: './policies-settings.page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PoliciesSettingsPage implements OnInit {
  private readonly api = inject(PublishingApiService);
  private readonly toast = inject(ToastService);
  private readonly confirm = inject(ConfirmDialogService);

  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly loadError = signal<string | null>(null);

  readonly sections = signal<PolicySection[]>([]);
  readonly editingId = signal<string | null>(null);

  readonly sectionForm = new FormGroup<SectionForm>({
    titleAr: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    titleEn: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    contentAr: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    contentEn: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
  });

  readonly cols = ['orderIndex', 'titleAr', 'titleEn', 'actions'];

  ngOnInit(): void { void this.load(); }

  async load(): Promise<void> {
    this.loading.set(true);
    this.loadError.set(null);
    const res = await this.api.getPoliciesSettings();
    this.loading.set(false);
    if (res.ok) {
      this.sections.set([...res.value.sections].sort((a, b) => a.orderIndex - b.orderIndex));
    } else {
      this.loadError.set(res.error.kind);
    }
  }

  startAdd(): void {
    this.editingId.set('new');
    this.sectionForm.reset();
  }

  startEdit(s: PolicySection): void {
    this.editingId.set(s.id);
    this.sectionForm.patchValue({ titleAr: s.titleAr, titleEn: s.titleEn, contentAr: s.contentAr, contentEn: s.contentEn });
  }

  cancel(): void { this.editingId.set(null); this.sectionForm.reset(); }

  async save(): Promise<void> {
    if (this.sectionForm.invalid) { this.sectionForm.markAllAsTouched(); return; }
    this.saving.set(true);
    const body = this.sectionForm.getRawValue();
    const id = this.editingId();
    const res = id === 'new'
      ? await this.api.createPolicySection(body)
      : await this.api.updatePolicySection(id!, body);
    this.saving.set(false);
    if (res.ok) {
      this.toast.success('policiesSettings.save.toast');
      this.cancel();
      void this.load();
    } else {
      this.toast.error(`errors.${res.error.kind}`);
    }
  }

  async delete(s: PolicySection): Promise<void> {
    if (!(await this.confirm.confirm({ titleKey: 'policiesSettings.delete.title', messageKey: 'policiesSettings.delete.message', confirmKey: 'policiesSettings.delete.confirm', cancelKey: 'common.actions.cancel' }))) return;
    const res = await this.api.deletePolicySection(s.id);
    if (res.ok) {
      this.toast.success('policiesSettings.delete.toast');
      void this.load();
    } else {
      this.toast.error(`errors.${res.error.kind}`);
    }
  }

  async moveUp(s: PolicySection): Promise<void> {
    const list = this.sections();
    const idx = list.findIndex((r) => r.id === s.id);
    if (idx <= 0) return;
    await this.reorder(s.id, idx - 1);
  }

  async moveDown(s: PolicySection): Promise<void> {
    const list = this.sections();
    const idx = list.findIndex((r) => r.id === s.id);
    if (idx < 0 || idx >= list.length - 1) return;
    await this.reorder(s.id, idx + 1);
  }

  private async reorder(id: string, newIndex: number): Promise<void> {
    const res = await this.api.reorderPolicySection(id, newIndex);
    if (res.ok) {
      this.toast.success('policiesSettings.reorder.toast');
      void this.load();
    } else {
      this.toast.error(`errors.${res.error.kind}`);
    }
  }
}
