import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatDividerModule } from '@angular/material/divider';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatSnackBar } from '@angular/material/snack-bar';
import { TranslateModule } from '@ngx-translate/core';

interface SettingsModel {
  general: {
    siteName: string;
    siteTagline: string;
    defaultLocale: 'ar' | 'en';
    timezone: string;
  };
  branding: {
    primaryColor: string;
    accentColor: string;
    logoUrl: string;
  };
  email: {
    smtpHost: string;
    smtpPort: number;
    smtpFromAddress: string;
    smtpFromName: string;
    enableTls: boolean;
  };
  security: {
    requireMfa: boolean;
    sessionTimeoutMinutes: number;
    enforcePasswordRotationDays: number;
    allowedSelfRegistration: boolean;
  };
  features: {
    publicRegistration: boolean;
    communityEnabled: boolean;
    knowledgeMapsEnabled: boolean;
    interactiveCityEnabled: boolean;
    smartAssistantEnabled: boolean;
  };
  storage: {
    maxUploadSizeMb: number;
    allowedFileTypes: string;
    cdnBaseUrl: string;
  };
}

const DEFAULT_SETTINGS: SettingsModel = {
  general: {
    siteName: 'CCE — Carbon Circular Economy',
    siteTagline: 'Knowledge for a Carbon Circular Economy',
    defaultLocale: 'ar',
    timezone: 'Asia/Riyadh',
  },
  branding: {
    primaryColor: '#006c4f',
    accentColor: '#c8a045',
    logoUrl: 'https://assets.cce.local/logo.svg',
  },
  email: {
    smtpHost: 'smtp.cce.local',
    smtpPort: 587,
    smtpFromAddress: 'no-reply@cce.local',
    smtpFromName: 'CCE Platform',
    enableTls: true,
  },
  security: {
    requireMfa: true,
    sessionTimeoutMinutes: 60,
    enforcePasswordRotationDays: 90,
    allowedSelfRegistration: true,
  },
  features: {
    publicRegistration: true,
    communityEnabled: true,
    knowledgeMapsEnabled: true,
    interactiveCityEnabled: true,
    smartAssistantEnabled: true,
  },
  storage: {
    maxUploadSizeMb: 50,
    allowedFileTypes: 'pdf,docx,xlsx,jpg,png,mp4',
    cdnBaseUrl: 'https://cdn.cce.local',
  },
};

/**
 * Platform Settings — global runtime configuration (BRD §4.1.28).
 *
 * Six logical sections (General · Branding · Email/SMTP · Security ·
 * Feature Flags · Storage). Demo-mode persists changes in component
 * state + a snackbar; real backend integration would PATCH
 * `/api/admin/settings` per section.
 */
@Component({
  selector: 'cce-settings',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatButtonModule,
    MatCheckboxModule,
    MatDividerModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatSelectModule,
    MatSlideToggleModule,
    TranslateModule,
  ],
  templateUrl: './settings.page.html',
  styleUrl: './settings.page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SettingsPage {
  readonly model = signal<SettingsModel>(structuredClone(DEFAULT_SETTINGS));
  readonly originalModel = signal<SettingsModel>(structuredClone(DEFAULT_SETTINGS));
  readonly activeSection = signal<keyof SettingsModel>('general');

  readonly sections: ReadonlyArray<{
    id: keyof SettingsModel;
    label: string;
    icon: string;
    description: string;
  }> = [
    { id: 'general',  label: 'General',       icon: 'tune',          description: 'Site identity, locale, timezone' },
    { id: 'branding', label: 'Branding',      icon: 'palette',       description: 'Colors, logo, visual identity' },
    { id: 'email',    label: 'Email / SMTP',  icon: 'mail',          description: 'Outgoing-mail configuration' },
    { id: 'security', label: 'Security',      icon: 'shield',        description: 'MFA, sessions, passwords' },
    { id: 'features', label: 'Feature Flags', icon: 'flag',          description: 'Toggle modules across the platform' },
    { id: 'storage',  label: 'Storage',       icon: 'cloud_upload',  description: 'Upload limits + CDN' },
  ];

  constructor(private readonly snack: MatSnackBar) {}

  isDirty(): boolean {
    return JSON.stringify(this.model()) !== JSON.stringify(this.originalModel());
  }

  setSection(id: keyof SettingsModel): void {
    this.activeSection.set(id);
  }

  updateGeneral<K extends keyof SettingsModel['general']>(key: K, value: SettingsModel['general'][K]): void {
    this.model.update((m) => ({ ...m, general: { ...m.general, [key]: value } }));
  }
  updateBranding<K extends keyof SettingsModel['branding']>(key: K, value: SettingsModel['branding'][K]): void {
    this.model.update((m) => ({ ...m, branding: { ...m.branding, [key]: value } }));
  }
  updateEmail<K extends keyof SettingsModel['email']>(key: K, value: SettingsModel['email'][K]): void {
    this.model.update((m) => ({ ...m, email: { ...m.email, [key]: value } }));
  }
  updateSecurity<K extends keyof SettingsModel['security']>(key: K, value: SettingsModel['security'][K]): void {
    this.model.update((m) => ({ ...m, security: { ...m.security, [key]: value } }));
  }
  updateFeatures<K extends keyof SettingsModel['features']>(key: K, value: SettingsModel['features'][K]): void {
    this.model.update((m) => ({ ...m, features: { ...m.features, [key]: value } }));
  }
  updateStorage<K extends keyof SettingsModel['storage']>(key: K, value: SettingsModel['storage'][K]): void {
    this.model.update((m) => ({ ...m, storage: { ...m.storage, [key]: value } }));
  }

  save(): void {
    if (!this.isDirty()) return;
    this.originalModel.set(structuredClone(this.model()));
    this.snack.open('Settings saved.', 'Dismiss', { duration: 3000 });
  }

  reset(): void {
    this.model.set(structuredClone(this.originalModel()));
    this.snack.open('Reverted unsaved changes.', 'Dismiss', { duration: 2000 });
  }

  restoreDefaults(): void {
    this.model.set(structuredClone(DEFAULT_SETTINGS));
    this.snack.open('Defaults restored. Click Save to commit.', 'Dismiss', { duration: 3000 });
  }
}
