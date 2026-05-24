import {
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  Component,
  ElementRef,
  OnDestroy,
  OnInit,
  inject,
  signal,
  viewChildren,
} from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { Subscription, interval } from 'rxjs';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { TranslocoModule } from '@jsverse/transloco';
import { AuthApiService } from '../../core/auth/auth-api.service';

type PageState = 'sending' | 'idle' | 'verifying' | 'error';

@Component({
  selector: 'cce-verify-phone',
  standalone: true,
  imports: [
    RouterLink,
    MatButtonModule,
    MatProgressSpinnerModule,
    TranslocoModule,
  ],
  templateUrl: './verify-phone.page.html',
  styleUrl: './verify-phone.page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class VerifyPhonePage implements OnInit, OnDestroy {
  private readonly router = inject(Router);
  private readonly authApi = inject(AuthApiService);
  private readonly cdr = inject(ChangeDetectorRef);

  readonly state = signal<PageState>('sending');
  readonly errorKey = signal<string>('');
  readonly countdown = signal<number>(0);
  readonly phoneNumber = signal<string>('');

  readonly digits = Array.from({ length: 6 }, () => signal(''));
  readonly otpInputs = viewChildren<ElementRef<HTMLInputElement>>('otpInput');

  verificationId = '';
  private countdownSub?: Subscription;

  constructor() {
    const nav = this.router.getCurrentNavigation();
    const phone = (nav?.extras.state as { phoneNumber?: string })?.phoneNumber ?? '';
    this.phoneNumber.set(phone);
  }

  ngOnInit(): void {
    if (!this.phoneNumber()) {
      this.router.navigate(['/register']);
      return;
    }
    this.sendOtp();
  }

  ngOnDestroy(): void {
    this.countdownSub?.unsubscribe();
  }

  get otpCode(): string {
    return this.digits.map(d => d()).join('');
  }

  get isComplete(): boolean {
    return this.digits.every(d => /^\d$/.test(d()));
  }

  onDigitInput(event: Event, index: number): void {
    const input = event.target as HTMLInputElement;
    const digit = input.value.replace(/\D/g, '').slice(-1);
    this.digits[index].set(digit);
    input.value = digit;
    if (digit && index < 5) this.focusBox(index + 1);
  }

  onKeyDown(event: KeyboardEvent, index: number): void {
    if (event.key === 'Backspace') {
      if (this.digits[index]()) {
        this.digits[index].set('');
      } else if (index > 0) {
        this.digits[index - 1].set('');
        this.focusBox(index - 1);
      }
    } else if (event.key === 'ArrowLeft' && index > 0) {
      this.focusBox(index - 1);
    } else if (event.key === 'ArrowRight' && index < 5) {
      this.focusBox(index + 1);
    }
  }

  onPaste(event: ClipboardEvent): void {
    event.preventDefault();
    const text = event.clipboardData?.getData('text') ?? '';
    const pasted = text.replace(/\D/g, '').slice(0, 6).split('');
    pasted.forEach((d, i) => this.digits[i]?.set(d));
    this.focusBox(Math.min(pasted.length, 5));
  }

  resend(): void {
    if (this.countdown() > 0 || this.state() === 'sending') return;
    this.digits.forEach(d => d.set(''));
    this.sendOtp();
  }

  submit(): void {
    if (!this.isComplete || this.state() === 'verifying') return;
    this.errorKey.set('');
    this.state.set('verifying');
    this.authApi.verifyPhoneOtp(this.verificationId, this.otpCode).subscribe({
      next: () => this.router.navigate(['/login']),
      error: () => {
        this.errorKey.set('account.verifyPhone.errorInvalid');
        this.state.set('idle');
      },
    });
  }

  private sendOtp(): void {
    this.state.set('sending');
    this.errorKey.set('');
    this.authApi.requestPhoneOtp(this.phoneNumber()).subscribe({
      next: (res) => {
        this.verificationId = res.verificationId;
        this.state.set('idle');
        this.startCountdown();
        setTimeout(() => this.focusBox(0));
      },
      error: (err: { apiCode?: string }) => {
        if (err?.apiCode === 'ERR124') {
          // Rate-limited — keep the OTP input visible, restart the countdown
          this.errorKey.set('account.verifyPhone.errorRateLimit');
          this.state.set(this.verificationId ? 'idle' : 'error');
          this.startCountdown();
        } else {
          this.errorKey.set('account.verifyPhone.errorGeneric');
          this.state.set('error');
        }
      },
    });
  }

  private focusBox(index: number): void {
    this.otpInputs()[index]?.nativeElement.focus();
  }

  private startCountdown(): void {
    this.countdownSub?.unsubscribe();
    this.countdown.set(60);
    this.countdownSub = interval(1000).subscribe(() => {
      const next = this.countdown() - 1;
      if (next <= 0) {
        this.countdown.set(0);
        this.countdownSub?.unsubscribe();
      } else {
        this.countdown.set(next);
      }
      this.cdr.markForCheck();
    });
  }
}
