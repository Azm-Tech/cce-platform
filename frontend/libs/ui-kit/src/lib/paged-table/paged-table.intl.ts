import { Injectable, OnDestroy } from '@angular/core';
import { MatPaginatorIntl } from '@angular/material/paginator';
import { TranslocoEvents, TranslocoService } from '@jsverse/transloco';
import { Subject, filter, takeUntil } from 'rxjs';

@Injectable()
export class TranslocoPaginatorIntl extends MatPaginatorIntl implements OnDestroy {
  private readonly destroy$ = new Subject<void>();

  constructor(private readonly translate: TranslocoService) {
    super();
    this.updateLabels();

    // Re-translate when the active language changes.
    this.translate.langChanges$.pipe(takeUntil(this.destroy$)).subscribe(() => {
      this.updateLabels();
      this.changes.next();
    });

    // Re-translate once the translation file finishes loading — handles the
    // case where the intl service is instantiated before translations are ready.
    this.translate.events$
      .pipe(
        filter((e: TranslocoEvents) => e.type === 'translationLoadSuccess'),
        takeUntil(this.destroy$),
      )
      .subscribe(() => {
        this.updateLabels();
        this.changes.next();
      });
  }

  private updateLabels(): void {
    this.itemsPerPageLabel = this.translate.translate('paginator.itemsPerPage');
    this.nextPageLabel     = this.translate.translate('paginator.nextPage');
    this.previousPageLabel = this.translate.translate('paginator.previousPage');
    this.firstPageLabel    = this.translate.translate('paginator.firstPage');
    this.lastPageLabel     = this.translate.translate('paginator.lastPage');
  }

  override getRangeLabel = (page: number, pageSize: number, length: number): string => {
    if (length === 0 || pageSize === 0) {
      return this.translate.translate('paginator.rangeEmpty', { length });
    }
    const start = page * pageSize + 1;
    const end = Math.min((page + 1) * pageSize, length);
    return this.translate.translate('paginator.range', { start, end, length });
  };

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }
}
