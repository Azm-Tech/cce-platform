import { ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslocoModule } from '@jsverse/transloco';
import { AuthToolbarComponent } from './auth-toolbar.component';
import { DevAuthService } from '../auth/dev-auth.service';

describe('AuthToolbarComponent', () => {
  let fixture: ComponentFixture<AuthToolbarComponent>;
  let component: AuthToolbarComponent;
  let auth: jest.Mocked<Pick<DevAuthService, 'signIn' | 'signOut'>>;

  beforeEach(async () => {
    auth = {
      signIn: jest.fn(),
      signOut: jest.fn(),
    } as unknown as jest.Mocked<Pick<DevAuthService, 'signIn' | 'signOut'>>;
    await TestBed.configureTestingModule({
      imports: [AuthToolbarComponent, TranslocoModule.forRoot()],
      providers: [{ provide: DevAuthService, useValue: auth }],
    }).compileComponents();
    fixture = TestBed.createComponent(AuthToolbarComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('signIn() delegates to DevAuthService', () => {
    component.signIn();
    expect(auth.signIn).toHaveBeenCalledTimes(1);
  });

  it('signOut() delegates to DevAuthService', () => {
    component.signOut();
    expect(auth.signOut).toHaveBeenCalledTimes(1);
  });
});
