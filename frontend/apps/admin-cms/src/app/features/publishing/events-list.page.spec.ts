import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { MatDialog } from '@angular/material/dialog';
import { TranslateModule } from '@ngx-translate/core';
import { of } from 'rxjs';
import { AuthService } from '../../core/auth/auth.service';
import { ConfirmDialogService } from '../../core/ui/confirm-dialog.service';
import { ToastService } from '../../core/ui/toast.service';
import { PublishingApiService, type Result } from './publishing-api.service';
import type { Event as CceEvent, PagedResult } from './publishing.types';
import { EventsListPage } from './events-list.page';

const EVENT: CceEvent = {
  id: 'e1',
  titleAr: 't-ar', titleEn: 't-en',
  descriptionAr: 'd-ar', descriptionEn: 'd-en',
  startsOn: '2026-04-29T10:00:00Z',
  endsOn: '2026-04-29T12:00:00Z',
  locationAr: null, locationEn: 'HQ',
  onlineMeetingUrl: null,
  featuredImageUrl: null,
  iCalUid: 'uid',
  rowVersion: 'v',
};

describe('EventsListPage', () => {
  let fixture: ComponentFixture<EventsListPage>;
  let page: EventsListPage;
  let listEvents: jest.Mock;
  let deleteEvent: jest.Mock;
  let confirm: { confirm: jest.Mock };
  let toast: { success: jest.Mock; error: jest.Mock };
  let dialog: { open: jest.Mock };
  let dialogRef: { afterClosed: jest.Mock };

  function ok(value: PagedResult<CceEvent>): Result<PagedResult<CceEvent>> {
    return { ok: true, value };
  }

  beforeEach(async () => {
    listEvents = jest.fn().mockResolvedValue(ok({ items: [EVENT], page: 1, pageSize: 20, total: 1 }));
    deleteEvent = jest.fn().mockResolvedValue({ ok: true, value: undefined });
    confirm = { confirm: jest.fn().mockResolvedValue(true) };
    toast = { success: jest.fn(), error: jest.fn() };
    dialogRef = { afterClosed: jest.fn().mockReturnValue(of(EVENT)) };
    dialog = { open: jest.fn().mockReturnValue(dialogRef) };

    await TestBed.configureTestingModule({
      imports: [EventsListPage, TranslateModule.forRoot()],
      providers: [
        provideNoopAnimations(),
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: PublishingApiService, useValue: { listEvents, deleteEvent } },
        { provide: ConfirmDialogService, useValue: confirm },
        { provide: ToastService, useValue: toast },
        { provide: MatDialog, useValue: dialog },
        { provide: AuthService, useValue: { currentUser: () => null, isAuthenticated: () => false, hasPermission: () => true } },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(EventsListPage);
    page = fixture.componentInstance;
  });

  it('loads on init', async () => {
    fixture.detectChanges();
    await fixture.whenStable();
    expect(listEvents).toHaveBeenCalled();
    expect(page.rows()).toEqual([EVENT]);
  });

  it('reschedule opens reschedule dialog', async () => {
    fixture.detectChanges();
    await fixture.whenStable();
    await page.reschedule(EVENT);
    expect(dialog.open).toHaveBeenCalled();
    expect(toast.success).toHaveBeenCalledWith('events.reschedule.toast');
  });

  it('delete confirms then DELETEs', async () => {
    fixture.detectChanges();
    await fixture.whenStable();
    await page.delete(EVENT);
    expect(deleteEvent).toHaveBeenCalledWith('e1');
    expect(toast.success).toHaveBeenCalledWith('events.delete.toast');
  });
});
