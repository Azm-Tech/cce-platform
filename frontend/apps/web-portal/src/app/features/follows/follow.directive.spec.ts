import { Component } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FollowDirective } from './follow.directive';
import { FollowsApiService, type Result } from './follows-api.service';
import { FollowsRegistryService } from './follows-registry.service';
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
    <button cceFollow type="topic" [entityId]="id"></button>
  `,
})
class HostComponent {
  id = 't2';
}

describe('FollowDirective', () => {
  let fixture: ComponentFixture<HostComponent>;
  let host: HostComponent;
  let api: { follow: jest.Mock; unfollow: jest.Mock; getMyFollows: jest.Mock };
  let registry: FollowsRegistryService;

  beforeEach(async () => {
    api = {
      follow: jest.fn().mockResolvedValue(ok(undefined)),
      unfollow: jest.fn().mockResolvedValue(ok(undefined)),
      getMyFollows: jest.fn().mockResolvedValue(ok(SAMPLE)),
    };

    await TestBed.configureTestingModule({
      imports: [HostComponent],
      providers: [
        FollowsRegistryService,
        { provide: FollowsApiService, useValue: api },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(HostComponent);
    host = fixture.componentInstance;
    registry = TestBed.inject(FollowsRegistryService);
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

  it('on follow() failure, optimistic flip is reverted', async () => {
    api.follow.mockResolvedValueOnce({ ok: false, error: { kind: 'server' } });
    fixture.detectChanges();
    await fixture.whenStable();
    const btn = fixture.nativeElement.querySelector('button') as HTMLButtonElement;
    btn.click();
    await fixture.whenStable();
    expect(api.follow).toHaveBeenCalled();
    // Reverted back to false (was never actually followed).
    expect(registry.isFollowing('topic', 't2')).toBe(false);
  });
});
