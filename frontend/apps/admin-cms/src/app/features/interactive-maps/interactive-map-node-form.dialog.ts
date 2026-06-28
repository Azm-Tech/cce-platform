import { ChangeDetectionStrategy, Component, Inject, OnInit, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { TranslocoModule } from '@jsverse/transloco';
import { TaxonomyApiService } from '../taxonomies/taxonomy-api.service';
import type { Topic } from '../taxonomies/taxonomy.types';
import { InteractiveMapsApiService } from './interactive-maps-api.service';
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
  category: FormControl<string>;
  categoryNameAr: FormControl<string>;
  categoryNameEn: FormControl<string>;
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
    TranslocoModule,
  ],
  templateUrl: './interactive-map-node-form.dialog.html',
  styleUrl: './interactive-map-node-form.dialog.scss',
  changeDetection: ChangeDetectionStrategy.Default,
})
export class InteractiveMapNodeFormDialogComponent implements OnInit {
  private readonly api = inject(InteractiveMapsApiService);
  private readonly taxonomyApi = inject(TaxonomyApiService);

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
      category: new FormControl(n?.category != null ? String(n.category) : '', { nonNullable: true }),
      categoryNameAr: new FormControl(n?.categoryNameAr ?? '', { nonNullable: true }),
      categoryNameEn: new FormControl(n?.categoryNameEn ?? '', { nonNullable: true }),
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
      category: v.category !== '' ? Number(v.category) : null,
      categoryNameAr: v.categoryNameAr || null,
      categoryNameEn: v.categoryNameEn || null,
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
