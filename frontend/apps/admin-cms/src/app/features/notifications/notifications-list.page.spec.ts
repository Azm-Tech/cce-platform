import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { MatDialog } from '@angular/material/dialog';
import { TranslateModule } from '@ngx-translate/core';
import { of } from 'rxjs';
import { AuthService } from '../../core/auth/auth.service';
import { ToastService } from '../../core/ui/toast.service';
import { NotificationApiService, type Result } from './notification-api.service';
import type { NotificationTemplate, PagedResult } from './notification.types';
import { NotificationsListPage } from './notifications-list.page';

const T: NotificationTemplate = {
  id: 't1', code: 'WelcomeEmail',
  subjectAr: 'a', subjectEn: 'b',
  bodyAr: 'c', bodyEn: 'd',
  channel: 'Email', variableSchemaJson: '{}', isActive: true,
};

describe('NotificationsListPage', () => {
  let fixture: ComponentFixture<NotificationsListPage>;
  let page: NotificationsListPage;
  let list: jest.Mock;
  let toast: { success: jest.Mock; error: jest.Mock };
  let dialog: { open: jest.Mock };
  let dialogRef: { afterClosed: jest.Mock };

  function ok(value: PagedResult<NotificationTemplate>): Result<PagedResult<NotificationTemplate>> {
    return { ok: true, value };
  }

  beforeEach(async () => {
    list = jest.fn().mockResolvedValue(ok({ items: [T], page: 1, pageSize: 20, total: 1 }));
    toast = { success: jest.fn(), error: jest.fn() };
    dialogRef = { afterClosed: jest.fn().mockReturnValue(of(T)) };
    dialog = { open: jest.fn().mockReturnValue(dialogRef) };

    await TestBed.configureTestingModule({
      imports: [NotificationsListPage, TranslateModule.forRoot()],
      providers: [
        provideNoopAnimations(),
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: NotificationApiService, useValue: { list } },
        { provide: ToastService, useValue: toast },
        { provide: MatDialog, useValue: dialog },
        { provide: AuthService, useValue: { currentUser: () => null, isAuthenticated: () => false, hasPermission: () => true } },
      ],
    }).compileComponents();
    fixture = TestBed.createComponent(NotificationsListPage);
    page = fixture.componentInstance;
  });

  it('loads on init', async () => {
    fixture.detectChanges();
    await fixture.whenStable();
    expect(list).toHaveBeenCalled();
    expect(page.rows()).toEqual([T]);
  });

  it('channel filter resets page + reloads', async () => {
    page.page.set(3);
    list.mockClear();
    page.onChannelFilter('Sms');
    await Promise.resolve();
    expect(page.page()).toBe(1);
    expect(list).toHaveBeenCalledWith({ page: 1, pageSize: 20, channel: 'Sms' });
  });

  it('openCreate opens dialog + toasts on success', async () => {
    fixture.detectChanges();
    await fixture.whenStable();
    await page.openCreate();
    expect(dialog.open).toHaveBeenCalled();
    expect(toast.success).toHaveBeenCalledWith('notifications.create.toast');
  });
});
