import { ChangeDetectionStrategy, Component, Inject, OnInit, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MAT_DIALOG_DATA, MatDialog, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';
import { firstValueFrom } from 'rxjs';
import { TranslocoModule } from '@jsverse/transloco';
import { iconDataUri, isCustomIconUrl } from '@frontend/ui-kit';
import { TaxonomyApiService } from '../taxonomies/taxonomy-api.service';
import type { Topic } from '../taxonomies/taxonomy.types';
import { InteractiveMapsApiService } from './interactive-maps-api.service';
import {
  InteractiveMapIconPickerDialogComponent,
  type IconPickerData,
} from './interactive-map-icon-picker.dialog';
import type { InteractiveMapNodeDto } from './interactive-maps.types';

export interface InteractiveMapNodeFormData {
  mapId: string;
  node?: InteractiveMapNodeDto;
  existingNodes: InteractiveMapNodeDto[];
}

interface NodeForm {
  nameAr: FormControl<string>;
  nameEn: FormControl<string>;
  iconKey: FormControl<string>;
  level: FormControl<number>;
  parentId: FormControl<string>;
  topicId: FormControl<string>;
  isActive: FormControl<boolean>;
}

@Component({
  selector: 'cce-interactive-map-node-form-dialog',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatButtonModule,
    MatCheckboxModule,
    MatDialogModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatSelectModule,
    MatProgressSpinnerModule,
    MatTooltipModule,
    TranslocoModule,
  ],
  templateUrl: './interactive-map-node-form.dialog.html',
  styleUrl: './interactive-map-node-form.dialog.scss',
  changeDetection: ChangeDetectionStrategy.Default,
})
export class InteractiveMapNodeFormDialogComponent implements OnInit {
  private readonly api = inject(InteractiveMapsApiService);
  private readonly taxonomyApi = inject(TaxonomyApiService);
  private readonly dialog = inject(MatDialog);

  readonly saving = signal(false);
  readonly errorKind = signal<string | null>(null);
  readonly topicsLoading = signal(true);
  readonly topics = signal<Topic[]>([]);
  readonly isEdit: boolean;
  readonly form: FormGroup<NodeForm>;

  /** Nodes that can serve as a parent (all other nodes in this map). */
  readonly parentCandidates: InteractiveMapNodeDto[];

  constructor(
    private readonly ref: MatDialogRef<InteractiveMapNodeFormDialogComponent, true | null>,
    @Inject(MAT_DIALOG_DATA) readonly data: InteractiveMapNodeFormData,
  ) {
    this.isEdit = data.node !== undefined;
    const n = data.node;
    this.parentCandidates = data.existingNodes.filter((nd) => nd.id !== n?.id);

    this.form = new FormGroup<NodeForm>({
      nameAr: new FormControl(n?.nameAr ?? '', { nonNullable: true, validators: [Validators.required] }),
      nameEn: new FormControl(n?.nameEn ?? '', { nonNullable: true, validators: [Validators.required] }),
      iconKey: new FormControl(n?.iconKey ?? '', { nonNullable: true }),
      level: new FormControl(n?.level ?? 0, { nonNullable: true, validators: [Validators.required, Validators.min(0)] }),
      parentId: new FormControl(n?.parentId ?? '', { nonNullable: true }),
      topicId: new FormControl(n?.topicId ?? '', { nonNullable: true, validators: [Validators.required] }),
      isActive: new FormControl(n?.isActive ?? true, { nonNullable: true }),
    });
  }

  async ngOnInit(): Promise<void> {
    const res = await this.taxonomyApi.listTopics({ pageSize: 200, isActive: true });
    this.topicsLoading.set(false);
    if (res.ok) this.topics.set(res.value.items);
  }

  // ── Icon picker ───────────────────────────────────────────────────────────
  /** Image source for the current icon (registry key or uploaded URL), or null. */
  iconPreview(): string | null {
    const key = this.form.controls.iconKey.value;
    return key ? iconDataUri(key) : null;
  }
  /** Whether the current icon is an uploaded file (vs a library key). */
  iconIsCustom(): boolean { return isCustomIconUrl(this.form.controls.iconKey.value); }
  /** Display label for the current icon: prettified key, "Custom", or empty. */
  iconLabel(): string {
    const key = this.form.controls.iconKey.value;
    if (!key) return '';
    if (isCustomIconUrl(key)) return '';
    const s = key.replace(/-/g, ' ');
    return s.charAt(0).toUpperCase() + s.slice(1);
  }

  async openIconPicker(): Promise<void> {
    const ref = this.dialog.open<
      InteractiveMapIconPickerDialogComponent,
      IconPickerData,
      string | undefined
    >(InteractiveMapIconPickerDialogComponent, {
      data: { current: this.form.controls.iconKey.value || null },
      width: '640px',
      maxWidth: '96vw',
      maxHeight: '90vh',
      autoFocus: false,
    });
    const result = await firstValueFrom(ref.afterClosed());
    if (typeof result === 'string') this.form.controls.iconKey.setValue(result);
  }

  clearIcon(): void { this.form.controls.iconKey.setValue(''); }

  async save(): Promise<void> {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    this.saving.set(true);
    this.errorKind.set(null);
    const v = this.form.getRawValue();
    const payload = {
      nameAr: v.nameAr || null,
      nameEn: v.nameEn || null,
      iconKey: v.iconKey || null,
      level: v.level,
      // Category is unused metadata and no longer editable — always sent null.
      category: null,
      categoryNameAr: null,
      categoryNameEn: null,
      parentId: v.parentId || null,
      topicId: v.topicId,
    };
    const res = this.isEdit && this.data.node
      ? await this.api.updateNode(this.data.mapId, this.data.node.id, { ...payload, isActive: v.isActive })
      : await this.api.createNode(this.data.mapId, payload);
    this.saving.set(false);
    if (res.ok) this.ref.close(true);
    else this.errorKind.set(res.error.kind);
  }

  cancel(): void { this.ref.close(null); }
}
