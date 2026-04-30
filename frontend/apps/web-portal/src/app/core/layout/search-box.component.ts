import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { TranslateModule } from '@ngx-translate/core';

@Component({
  selector: 'cce-search-box',
  standalone: true,
  imports: [FormsModule, MatFormFieldModule, MatIconModule, MatInputModule, TranslateModule],
  template: `
    <mat-form-field appearance="outline" class="cce-search-box">
      <mat-icon matPrefix>search</mat-icon>
      <input matInput type="search"
        [placeholder]="'search.placeholder' | translate"
        [ngModel]="query()" (ngModelChange)="query.set($event)"
        (keyup.enter)="submit()" />
    </mat-form-field>
  `,
  styleUrl: './search-box.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SearchBoxComponent {
  private readonly router = inject(Router);
  readonly query = signal('');
  submit(): void {
    const q = this.query().trim();
    if (q) void this.router.navigate(['/search'], { queryParams: { q } });
  }
}
