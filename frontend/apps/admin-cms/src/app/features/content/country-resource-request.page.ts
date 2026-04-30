import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { TranslateModule } from '@ngx-translate/core';
import { ToastService } from '@frontend/ui-kit';
import { ContentApiService } from './content-api.service';

const GUID_RE = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

interface CrrForm {
  requestId: FormControl<string>;
  adminNotesAr: FormControl<string>;
  adminNotesEn: FormControl<string>;
}

/**
 * Admin → Country resource requests. The Internal API only exposes approve
 * + reject (no list endpoint) so v0.1.0 ships a by-ID power-user form. A
 * future phase can add a list once Sub-3 exposes one.
 */
@Component({
  selector: 'cce-country-resource-request',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatButtonModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    TranslateModule,
  ],
  templateUrl: './country-resource-request.page.html',
  styleUrl: './country-resource-request.page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CountryResourceRequestPage {
  private readonly api = inject(ContentApiService);
  private readonly toast = inject(ToastService);

  readonly form = new FormGroup<CrrForm>({
    requestId: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.pattern(GUID_RE)] }),
    adminNotesAr: new FormControl('', { nonNullable: true }),
    adminNotesEn: new FormControl('', { nonNullable: true }),
  });
  readonly busy = signal(false);

  async approve(): Promise<void> {
    if (this.form.controls.requestId.invalid) {
      this.form.controls.requestId.markAsTouched();
      return;
    }
    this.busy.set(true);
    const v = this.form.getRawValue();
    const res = await this.api.approveCountryResourceRequest(v.requestId, {
      adminNotesAr: v.adminNotesAr || null,
      adminNotesEn: v.adminNotesEn || null,
    });
    this.busy.set(false);
    if (res.ok) {
      this.toast.success('countryResourceRequest.approve.toast');
      this.form.reset();
    } else {
      this.toast.error(`errors.${res.error.kind}`);
    }
  }

  async reject(): Promise<void> {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    if (!this.form.controls.adminNotesAr.value || !this.form.controls.adminNotesEn.value) {
      this.toast.error('countryResourceRequest.reject.notesRequired');
      return;
    }
    this.busy.set(true);
    const v = this.form.getRawValue();
    const res = await this.api.rejectCountryResourceRequest(v.requestId, {
      adminNotesAr: v.adminNotesAr,
      adminNotesEn: v.adminNotesEn,
    });
    this.busy.set(false);
    if (res.ok) {
      this.toast.success('countryResourceRequest.reject.toast');
      this.form.reset();
    } else {
      this.toast.error(`errors.${res.error.kind}`);
    }
  }
}
