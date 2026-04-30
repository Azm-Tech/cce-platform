import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { signal } from '@angular/core';
import { LocaleService } from '@frontend/i18n';
import { ToastService } from '@frontend/ui-kit';
import { TranslateModule } from '@ngx-translate/core';
import { NotificationsApiService, type Result } from './notifications-api.service';
import type { PagedResult, UserNotification } from './notification.types';
import { NotificationsDrawerComponent } from './notifications-drawer.component';

const UNREAD: UserNotification = {
  id: 'n1',
  templateId: 't1',
  renderedSubjectAr: 'عنوان', renderedSubjectEn: 'Subject',
  renderedBody: 'Body 1',
  renderedLocale: 'en',
  channel: 'InApp',
  sentOn: '2026-04-29T12:00:00Z',
  readOn: null,
  status: 'Sent',
};

const READ: UserNotification = { ...UNREAD, id: 'n2', status: 'Read', readOn: '2026-04-29T13:00:00Z' };

describe('NotificationsDrawerComponent', () => {
  let fixture: ComponentFixture<NotificationsDrawerComponent>;
  let component: NotificationsDrawerComponent;
  let list: jest.Mock;
  let markRead: jest.Mock;
  let markAllRead: jest.Mock;
  let toastSuccess: jest.Mock;
  let unreadEmits: number[];

  function ok<T>(value: T): Result<T> {
    return { ok: true, value };
  }

  beforeEach(async () => {
    list = jest.fn().mockResolvedValue(
      ok({ items: [UNREAD, READ], page: 1, pageSize: 10, total: 2 } as PagedResult<UserNotification>),
    );
    markRead = jest.fn().mockResolvedValue(ok(undefined));
    markAllRead = jest.fn().mockResolvedValue(ok(1));
    toastSuccess = jest.fn();

    const localeSig = signal<'ar' | 'en'>('en');

    await TestBed.configureTestingModule({
      imports: [NotificationsDrawerComponent, TranslateModule.forRoot()],
      providers: [
        provideNoopAnimations(),
        {
          provide: NotificationsApiService,
          useValue: { list, markRead, markAllRead, getUnreadCount: jest.fn() },
        },
        { provide: LocaleService, useValue: { locale: localeSig.asReadonly() } },
        { provide: ToastService, useValue: { success: toastSuccess, error: jest.fn() } },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(NotificationsDrawerComponent);
    component = fixture.componentInstance;
    unreadEmits = [];
    component.unreadCountChange.subscribe((n) => unreadEmits.push(n));
  });

  it('refresh() loads rows and emits unread count', async () => {
    await component.refresh();
    expect(list).toHaveBeenCalledWith({ page: 1, pageSize: 10 });
    expect(component.rows()).toHaveLength(2);
    expect(unreadEmits[unreadEmits.length - 1]).toBe(1);
  });

  it('onMarkRead(id) marks the row Read locally + emits new unread', async () => {
    await component.refresh();
    unreadEmits.length = 0;
    await component.onMarkRead('n1');
    expect(markRead).toHaveBeenCalledWith('n1');
    expect(component.rows().find((r) => r.id === 'n1')?.status).toBe('Read');
    expect(unreadEmits[unreadEmits.length - 1]).toBe(0);
  });

  it('onMarkAllRead transitions all unread rows to Read + emits 0', async () => {
    await component.refresh();
    unreadEmits.length = 0;
    await component.onMarkAllRead();
    expect(markAllRead).toHaveBeenCalled();
    expect(component.rows().every((r) => r.status === 'Read')).toBe(true);
    expect(toastSuccess).toHaveBeenCalledWith('notifications.markedToast', { n: 1 });
    expect(unreadEmits[unreadEmits.length - 1]).toBe(0);
  });

  it('onPage updates page+pageSize and re-fires list', async () => {
    await component.refresh();
    list.mockClear();
    component.onPage({ pageIndex: 1, pageSize: 20, length: 2, previousPageIndex: 0 });
    await Promise.resolve();
    expect(component.page()).toBe(2);
    expect(component.pageSize()).toBe(20);
    expect(list).toHaveBeenCalledWith({ page: 2, pageSize: 20 });
  });

  it('error path sets errorKind, retry triggers fresh refresh', async () => {
    list.mockResolvedValueOnce({ ok: false, error: { kind: 'server' } });
    await component.refresh();
    expect(component.errorKind()).toBe('server');
    list.mockClear();
    list.mockResolvedValueOnce(
      ok({ items: [READ], page: 1, pageSize: 10, total: 1 } as PagedResult<UserNotification>),
    );
    component.retry();
    await Promise.resolve();
    expect(list).toHaveBeenCalled();
  });

  it('empty result triggers empty() computed', async () => {
    list.mockResolvedValueOnce(
      ok({ items: [], page: 1, pageSize: 10, total: 0 } as PagedResult<UserNotification>),
    );
    await component.refresh();
    expect(component.empty()).toBe(true);
  });
});
