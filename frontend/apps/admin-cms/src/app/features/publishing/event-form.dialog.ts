import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, Inject, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { TranslateModule } from '@ngx-translate/core';
import { PublishingApiService } from './publishing-api.service';
import type { Event } from './publishing.types';

export interface EventFormDialogData {
  event?: Event;
}

interface EventForm {
  titleAr: FormControl<string>;
  titleEn: FormControl<string>;
  descriptionAr: FormControl<string>;
  descriptionEn: FormControl<string>;
  startsOn: FormControl<string>;
  endsOn: FormControl<string>;
  locationAr: FormControl<string>;
  locationEn: FormControl<string>;
  onlineMeetingUrl: FormControl<string>;
  featuredImageUrl: FormControl<string>;
}

@Component({
  selector: 'cce-event-form-dialog',
  standalone: true,
  imports: [
    CommonModule, ReactiveFormsModule, MatButtonModule, MatDialogModule,
    MatFormFieldModule, MatInputModule, MatProgressSpinnerModule, TranslateModule,
  ],
  templateUrl: './event-form.dialog.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class EventFormDialogComponent {
  private readonly api = inject(PublishingApiService);
  readonly form: FormGroup<EventForm>;
  readonly saving = signal(false);
  readonly errorKind = signal<string | null>(null);
  readonly isEdit: boolean;

  constructor(
    private readonly ref: MatDialogRef<EventFormDialogComponent, Event | null>,
    @Inject(MAT_DIALOG_DATA) readonly data: EventFormDialogData,
  ) {
    this.isEdit = data.event !== undefined;
    const e = data.event;
    this.form = new FormGroup<EventForm>({
      titleAr: new FormControl(e?.titleAr ?? '', { nonNullable: true, validators: [Validators.required] }),
      titleEn: new FormControl(e?.titleEn ?? '', { nonNullable: true, validators: [Validators.required] }),
      descriptionAr: new FormControl(e?.descriptionAr ?? '', { nonNullable: true, validators: [Validators.required] }),
      descriptionEn: new FormControl(e?.descriptionEn ?? '', { nonNullable: true, validators: [Validators.required] }),
      startsOn: new FormControl(e?.startsOn ?? '', { nonNullable: true, validators: [Validators.required] }),
      endsOn: new FormControl(e?.endsOn ?? '', { nonNullable: true, validators: [Validators.required] }),
      locationAr: new FormControl(e?.locationAr ?? '', { nonNullable: true }),
      locationEn: new FormControl(e?.locationEn ?? '', { nonNullable: true }),
      onlineMeetingUrl: new FormControl(e?.onlineMeetingUrl ?? '', { nonNullable: true }),
      featuredImageUrl: new FormControl(e?.featuredImageUrl ?? '', { nonNullable: true }),
    });
  }

  async save(): Promise<void> {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.saving.set(true);
    this.errorKind.set(null);
    const v = this.form.getRawValue();
    const nullify = (s: string) => s || null;

    if (this.isEdit && this.data.event) {
      const res = await this.api.updateEvent(this.data.event.id, {
        titleAr: v.titleAr,
        titleEn: v.titleEn,
        descriptionAr: v.descriptionAr,
        descriptionEn: v.descriptionEn,
        locationAr: nullify(v.locationAr),
        locationEn: nullify(v.locationEn),
        onlineMeetingUrl: nullify(v.onlineMeetingUrl),
        featuredImageUrl: nullify(v.featuredImageUrl),
        rowVersion: this.data.event.rowVersion,
      });
      this.saving.set(false);
      if (res.ok) this.ref.close(res.value);
      else this.errorKind.set(res.error.kind);
    } else {
      const res = await this.api.createEvent({
        titleAr: v.titleAr,
        titleEn: v.titleEn,
        descriptionAr: v.descriptionAr,
        descriptionEn: v.descriptionEn,
        startsOn: v.startsOn,
        endsOn: v.endsOn,
        locationAr: nullify(v.locationAr),
        locationEn: nullify(v.locationEn),
        onlineMeetingUrl: nullify(v.onlineMeetingUrl),
        featuredImageUrl: nullify(v.featuredImageUrl),
      });
      this.saving.set(false);
      if (res.ok) this.ref.close(res.value);
      else this.errorKind.set(res.error.kind);
    }
  }

  cancel(): void { this.ref.close(null); }
}
