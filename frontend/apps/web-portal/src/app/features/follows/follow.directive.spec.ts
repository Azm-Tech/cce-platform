import { Component } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ToastService } from '@frontend/ui-kit';
import { FollowDirective } from './follow.directive';
import { FollowsApiService, type Result } from './follows-api.service';
import { FollowsStoreService } from './follows-store.service';
import type { MyFollows } from './follows.types';

const SAMPLE: MyFollows = {
  topicIds: ['t1'],
  userIds: [],
  postIds: [],
};

function ok<T>(value: T): Result<T> {
  return { ok: true, value };
}

@Component({
  standalone: true,
  imports: [FollowDirective],
  template: `
    <button type="button" cceFollow entityType="topic" [entityId]="id">Toggle</button>
  `,
})
class HostComponent {
  id = 't2';
}

describe('FollowDirective', () => {
  let fixture: ComponentFixture<HostComponent>;
  let host: HostComponent;
  let api: { follow: jest.Mock; unfollow: jest.Mock; getMyFollows: jest.Mock };
  let registry: FollowsStoreService;
  let toast: { success: jest.Mock; error: jest.Mock };

  beforeEach(async () => {
    api = {
      follow: jest.fn().mockResolvedValue(ok(undefined)),
      unfollow: jest.fn().mockResolvedValue(ok(undefined)),
      getMyFollows: jest.fn().mockResolvedValue(ok(SAMPLE)),
    };

    toast = {
      success: jest.fn(),
      error: jest.fn(),
    };

    await TestBed.configureTestingModule({
      imports: [HostComponent],
      providers: [
        FollowsStoreService,
        { provide: FollowsApiService, useValue: api },
        { provide: ToastService, useValue: toast },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(HostComponent);
    host = fixture.componentInstance;
    registry = TestBed.inject(FollowsStoreService);
  });

  it('ngOnInit calls registry.ensureLoaded (which fetches once)', async () => {
    fixture.detectChanges();
    await fixture.whenStable();
    expect(api.getMyFollows).toHaveBeenCalledTimes(1);
    expect(registry.state()).toEqual(SAMPLE);
  });

  it('click on a not-yet-followed entity calls follow() and sets state to true', async () => {
    fixture.detectChanges();
    await fixture.whenStable();
    expect(registry.isFollowing('topic', 't2')).toBe(false);
    const btn = fixture.nativeElement.querySelector('button') as HTMLButtonElement;
    btn.click();
    await fixture.whenStable();
    expect(api.follow).toHaveBeenCalledWith('topic', 't2');
    expect(registry.isFollowing('topic', 't2')).toBe(true);
  });

  it('shows success toast when following a topic', async () => {
    fixture.detectChanges();
    await fixture.whenStable();
    const btn = fixture.nativeElement.querySelector('button') as HTMLButtonElement;
    btn.click();
    await fixture.whenStable();
    expect(toast.success).toHaveBeenCalledWith('confirmations.CON010');
    expect(toast.error).not.toHaveBeenCalled();
  });

  it('does not show toast when unfollowing', async () => {
    host.id = 't1';
    fixture.detectChanges();
    await fixture.whenStable();
    const btn = fixture.nativeElement.querySelector('button') as HTMLButtonElement;
    btn.click();
    await fixture.whenStable();
    expect(api.unfollow).toHaveBeenCalledWith('topic', 't1');
    expect(registry.isFollowing('topic', 't1')).toBe(false);
    expect(toast.success).not.toHaveBeenCalled();
  });

  it('click on an already-followed entity calls unfollow() and clears state', async () => {
    host.id = 't1';
    fixture.detectChanges();
    await fixture.whenStable();
    expect(registry.isFollowing('topic', 't1')).toBe(true);
    const btn = fixture.nativeElement.querySelector('button') as HTMLButtonElement;
    btn.click();
    await fixture.whenStable();
    expect(api.unfollow).toHaveBeenCalledWith('topic', 't1');
    expect(registry.isFollowing('topic', 't1')).toBe(false);
  });

  it('on follow() failure, optimistic flip is reverted and error toast shown', async () => {
    api.follow.mockResolvedValueOnce({ ok: false, error: { kind: 'server' } });
    fixture.detectChanges();
    await fixture.whenStable();
    const btn = fixture.nativeElement.querySelector('button') as HTMLButtonElement;
    btn.click();
    await fixture.whenStable();
    expect(api.follow).toHaveBeenCalled();
    expect(registry.isFollowing('topic', 't2')).toBe(false);
    expect(toast.error).toHaveBeenCalledWith('errors.ERR012');
  });

  it('on unfollow() failure, optimistic flip is reverted and error toast shown', async () => {
    host.id = 't1';
    api.unfollow.mockResolvedValueOnce({ ok: false, error: { kind: 'server' } });
    fixture.detectChanges();
    await fixture.whenStable();
    const btn = fixture.nativeElement.querySelector('button') as HTMLButtonElement;
    btn.click();
    await fixture.whenStable();
    expect(api.unfollow).toHaveBeenCalled();
    expect(registry.isFollowing('topic', 't1')).toBe(true);
    expect(toast.error).toHaveBeenCalledWith('errors.ERR012');
  });
});
