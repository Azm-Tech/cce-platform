import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { TranslocoModule } from '@jsverse/transloco';
import { ConfirmDialogService, ToastService } from '@frontend/ui-kit';
import { PublishingApiService } from './publishing-api.service';
import type { AboutSettings, GlossaryTerm, KnowledgePartner } from './publishing.types';

interface DescForm {
  descriptionAr: FormControl<string>;
  descriptionEn: FormControl<string>;
  howToUseVideoUrl: FormControl<string>;
}

interface GlossaryForm {
  termAr: FormControl<string>;
  termEn: FormControl<string>;
  definitionAr: FormControl<string>;
  definitionEn: FormControl<string>;
}

interface PartnerForm {
  nameAr: FormControl<string>;
  nameEn: FormControl<string>;
  logoUrl: FormControl<string>;
  websiteUrl: FormControl<string>;
  descriptionAr: FormControl<string>;
  descriptionEn: FormControl<string>;
}

@Component({
  selector: 'cce-about-settings',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatIconModule,
    MatProgressBarModule,
    TranslocoModule,
  ],
  templateUrl: './about-settings.page.html',
  styleUrl: './about-settings.page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AboutSettingsPage implements OnInit {
  private readonly api = inject(PublishingApiService);
  private readonly toast = inject(ToastService);
  private readonly confirm = inject(ConfirmDialogService);

  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly loadError = signal<string | null>(null);

  readonly glossaryTerms = signal<GlossaryTerm[]>([]);
  readonly partners = signal<KnowledgePartner[]>([]);
  readonly editingTermId = signal<string | null>(null);
  readonly editingPartnerId = signal<string | null>(null);

  readonly descForm = new FormGroup<DescForm>({
    descriptionAr: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.maxLength(1000)] }),
    descriptionEn: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.maxLength(1000)] }),
    howToUseVideoUrl: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
  });

  readonly glossaryForm = new FormGroup<GlossaryForm>({
    termAr:       new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.maxLength(100)] }),
    termEn:       new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.maxLength(100)] }),
    definitionAr: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.maxLength(1000)] }),
    definitionEn: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.maxLength(1000)] }),
  });

  readonly partnerForm = new FormGroup<PartnerForm>({
    nameAr:        new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    nameEn:        new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    logoUrl:       new FormControl('', { nonNullable: true }),
    websiteUrl:    new FormControl('', { nonNullable: true }),
    descriptionAr: new FormControl('', { nonNullable: true }),
    descriptionEn: new FormControl('', { nonNullable: true }),
  });

  ngOnInit(): void { void this.load(); }

  async load(): Promise<void> {
    this.loading.set(true);
    this.loadError.set(null);
    const res = await this.api.getAboutSettings();
    this.loading.set(false);
    if (res.ok) {
      this.applySettings(res.value);
    } else {
      this.loadError.set(res.error.kind);
    }
  }

  private applySettings(s: AboutSettings): void {
    this.descForm.patchValue({
      descriptionAr:   s.descriptionAr ?? '',
      descriptionEn:   s.descriptionEn ?? '',
      howToUseVideoUrl: s.howToUseVideoUrl ?? '',
    });
    this.glossaryTerms.set(s.glossaryTerms ?? []);
    this.partners.set(s.knowledgePartners ?? []);
  }

  async saveDescription(): Promise<void> {
    if (this.descForm.invalid) { this.descForm.markAllAsTouched(); return; }
    this.saving.set(true);
    const v = this.descForm.getRawValue();
    const res = await this.api.updateAboutSettings({
      descriptionAr:    v.descriptionAr,
      descriptionEn:    v.descriptionEn,
      howToUseVideoUrl: v.howToUseVideoUrl || null,
    });
    this.saving.set(false);
    if (res.ok) this.toast.success('aboutSettings.save.toast');
    else        this.toast.error(`errors.${res.error.kind}`);
  }

  // ── Glossary ────────────────────────────────────────────────────────────

  startAddTerm(): void { this.editingTermId.set('new'); this.glossaryForm.reset(); }

  startEditTerm(t: GlossaryTerm): void {
    this.editingTermId.set(t.id);
    this.glossaryForm.patchValue({ termAr: t.termAr, termEn: t.termEn, definitionAr: t.definitionAr, definitionEn: t.definitionEn });
  }

  cancelTerm(): void { this.editingTermId.set(null); this.glossaryForm.reset(); }

  async saveTerm(): Promise<void> {
    if (this.glossaryForm.invalid) { this.glossaryForm.markAllAsTouched(); return; }
    this.saving.set(true);
    const body = this.glossaryForm.getRawValue();
    const id   = this.editingTermId();
    const res  = id === 'new' ? await this.api.createGlossaryTerm(body) : await this.api.updateGlossaryTerm(id!, body);
    this.saving.set(false);
    if (res.ok) { this.toast.success('aboutSettings.glossary.save.toast'); this.cancelTerm(); void this.load(); }
    else         this.toast.error(`errors.${res.error.kind}`);
  }

  async deleteTerm(t: GlossaryTerm): Promise<void> {
    if (!(await this.confirm.confirm({ titleKey: 'aboutSettings.glossary.delete.title', messageKey: 'aboutSettings.glossary.delete.message', confirmKey: 'aboutSettings.glossary.delete.confirm', cancelKey: 'common.actions.cancel' }))) return;
    const res = await this.api.deleteGlossaryTerm(t.id);
    if (res.ok) { this.toast.success('aboutSettings.glossary.delete.toast'); void this.load(); }
    else         this.toast.error(`errors.${res.error.kind}`);
  }

  // ── Knowledge Partners ──────────────────────────────────────────────────

  startAddPartner(): void { this.editingPartnerId.set('new'); this.partnerForm.reset(); }

  startEditPartner(p: KnowledgePartner): void {
    this.editingPartnerId.set(p.id);
    this.partnerForm.patchValue({ nameAr: p.nameAr, nameEn: p.nameEn, logoUrl: p.logoUrl ?? '', websiteUrl: p.websiteUrl ?? '', descriptionAr: p.descriptionAr ?? '', descriptionEn: p.descriptionEn ?? '' });
  }

  cancelPartner(): void { this.editingPartnerId.set(null); this.partnerForm.reset(); }

  async savePartner(): Promise<void> {
    if (this.partnerForm.invalid) { this.partnerForm.markAllAsTouched(); return; }
    this.saving.set(true);
    const v   = this.partnerForm.getRawValue();
    const body = { nameAr: v.nameAr, nameEn: v.nameEn, logoUrl: v.logoUrl || null, websiteUrl: v.websiteUrl || null, descriptionAr: v.descriptionAr || null, descriptionEn: v.descriptionEn || null };
    const id   = this.editingPartnerId();
    const res  = id === 'new' ? await this.api.createKnowledgePartner(body) : await this.api.updateKnowledgePartner(id!, body);
    this.saving.set(false);
    if (res.ok) { this.toast.success('aboutSettings.partners.save.toast'); this.cancelPartner(); void this.load(); }
    else         this.toast.error(`errors.${res.error.kind}`);
  }

  async deletePartner(p: KnowledgePartner): Promise<void> {
    if (!(await this.confirm.confirm({ titleKey: 'aboutSettings.partners.delete.title', messageKey: 'aboutSettings.partners.delete.message', confirmKey: 'aboutSettings.partners.delete.confirm', cancelKey: 'common.actions.cancel' }))) return;
    const res = await this.api.deleteKnowledgePartner(p.id);
    if (res.ok) { this.toast.success('aboutSettings.partners.delete.toast'); void this.load(); }
    else         this.toast.error(`errors.${res.error.kind}`);
  }
}
