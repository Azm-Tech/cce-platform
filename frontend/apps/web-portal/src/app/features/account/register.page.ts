import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormsModule, NgForm } from '@angular/forms';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { TranslateModule } from '@ngx-translate/core';
import { AuthService } from '../../core/auth/auth.service';

interface RegistrationFormModel {
  givenName: string;
  surname: string;
  email: string;
  mailNickname: string;
}

type SubmitState =
  | { kind: 'idle' }
  | { kind: 'submitting' }
  | { kind: 'success'; userPrincipalName: string }
  | { kind: 'error'; messageKey: string };

/**
 * Public registration page.
 *
 * Sub-11d — anonymous self-service is back. Sub-11 Phase 01 made the
 * /api/users/register endpoint admin-only as a stop-gap until an
 * IEmailSender abstraction existed. Sub-11d Tasks A+B added it; the
 * temp password is now delivered via email (subject "Welcome to CCE")
 * instead of returned in the response. The page now POSTs the form
 * directly to /api/users/register and surfaces 201 as "check your
 * inbox", 409 as "account already exists", 400 as field errors.
 *
 * Internal-tenant users (cce.local synced via Entra ID Connect) and
 * partner-tenant users should sign in with their existing accounts;
 * the form below is for external users who don't have an Entra ID
 * account anywhere.
 */
@Component({
  selector: 'cce-register',
  standalone: true,
  imports: [
    CommonModule,
    RouterLink,
    FormsModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    TranslateModule,
  ],
  template: `
    <section class="cce-register">
      <h1 class="cce-register__title">{{ 'account.register.title' | translate }}</h1>

      @if (isAuthenticated()) {
        <p class="cce-register__body">{{ 'account.register.alreadySignedIn' | translate }}</p>
        <a mat-flat-button color="primary" routerLink="/me/profile">
          {{ 'account.register.openProfile' | translate }}
        </a>
      } @else {
        @if (state().kind === 'success') {
          <p class="cce-register__body">
            {{ 'account.register.successBody' | translate }}
          </p>
          <button type="button" mat-flat-button color="primary" (click)="signIn()">
            {{ 'account.register.signInButton' | translate }}
          </button>
        } @else {
          <p class="cce-register__body">{{ 'account.register.body' | translate }}</p>

          <form #form="ngForm" class="cce-register__form" (ngSubmit)="submit(form)">
            <mat-form-field appearance="outline">
              <mat-label>{{ 'account.register.givenNameLabel' | translate }}</mat-label>
              <input
                matInput
                name="givenName"
                [(ngModel)]="model.givenName"
                required
                autocomplete="given-name"
              />
            </mat-form-field>

            <mat-form-field appearance="outline">
              <mat-label>{{ 'account.register.surnameLabel' | translate }}</mat-label>
              <input
                matInput
                name="surname"
                [(ngModel)]="model.surname"
                required
                autocomplete="family-name"
              />
            </mat-form-field>

            <mat-form-field appearance="outline">
              <mat-label>{{ 'account.register.emailLabel' | translate }}</mat-label>
              <input
                matInput
                name="email"
                type="email"
                [(ngModel)]="model.email"
                required
                autocomplete="email"
              />
            </mat-form-field>

            <mat-form-field appearance="outline">
              <mat-label>{{ 'account.register.mailNicknameLabel' | translate }}</mat-label>
              <input
                matInput
                name="mailNickname"
                [(ngModel)]="model.mailNickname"
                required
                autocomplete="username"
              />
              <mat-hint>{{ 'account.register.mailNicknameHint' | translate }}</mat-hint>
            </mat-form-field>

            @if (state().kind === 'error') {
              <p class="cce-register__error" role="alert">
                {{ errorMessageKey() | translate }}
              </p>
            }

            <button
              type="submit"
              mat-flat-button
              color="primary"
              [disabled]="state().kind === 'submitting' || form.invalid"
            >
              {{ submitButtonKey() | translate }}
            </button>
          </form>

          <p class="cce-register__hint">{{ 'account.register.contactHint' | translate }}</p>
          <button
            type="button"
            mat-button
            class="cce-register__signin-link"
            (click)="signIn()"
          >
            {{ 'account.register.signInExistingButton' | translate }}
          </button>
        }
      }
    </section>
  `,
  styleUrl: './register.page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RegisterPage {
  private readonly auth = inject(AuthService);
  private readonly http = inject(HttpClient);

  readonly isAuthenticated = this.auth.isAuthenticated;
  readonly state = signal<SubmitState>({ kind: 'idle' });

  model: RegistrationFormModel = {
    givenName: '',
    surname: '',
    email: '',
    mailNickname: '',
  };

  submit(form: NgForm): void {
    if (form.invalid || this.state().kind === 'submitting') {
      return;
    }
    this.state.set({ kind: 'submitting' });
    this.http
      .post<{ entraIdObjectId: string; userPrincipalName: string }>(
        '/api/users/register',
        this.model,
      )
      .subscribe({
        next: (response) =>
          this.state.set({ kind: 'success', userPrincipalName: response.userPrincipalName }),
        error: (err: HttpErrorResponse) => {
          if (err.status === 409) {
            this.state.set({ kind: 'error', messageKey: 'account.register.errorConflict' });
          } else if (err.status === 400) {
            this.state.set({ kind: 'error', messageKey: 'account.register.errorValidation' });
          } else {
            this.state.set({ kind: 'error', messageKey: 'account.register.errorGeneric' });
          }
        },
      });
  }

  signIn(): void {
    this.auth.signIn('/me/profile');
  }

  errorMessageKey(): string {
    const s = this.state();
    return s.kind === 'error' ? s.messageKey : '';
  }

  submitButtonKey(): string {
    return this.state().kind === 'submitting'
      ? 'account.register.submittingButton'
      : 'account.register.submitButton';
  }
}
