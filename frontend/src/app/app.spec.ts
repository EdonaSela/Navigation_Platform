import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { vi } from 'vitest';
import { App } from './app';
import { AuthService } from './services/auth.service';

describe('App', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [App],
      providers: [
        provideRouter([]),
        {
          provide: AuthService,
          useValue: {
            isAdmin: () => false
          }
        }
      ]
    }).compileComponents();
  });

  it('should create the app', () => {
    const fixture = TestBed.createComponent(App);
    const app = fixture.componentInstance;
    expect(app).toBeTruthy();
  });

  it('should render app shell', async () => {
    const fixture = TestBed.createComponent(App);
    await fixture.whenStable();
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('.brand-text')?.textContent).toContain('Navigation Platform');
  });

  it('should clear storage and redirect on logout', () => {
    const fixture = TestBed.createComponent(App);
    const app = fixture.componentInstance;
    const clearSpy = vi.spyOn(Storage.prototype, 'clear');
    const redirectSpy = vi.spyOn(app as any, 'redirectTo');

    app.logout();

    expect(clearSpy).toHaveBeenCalledTimes(2);
    expect(redirectSpy).toHaveBeenCalledWith('/api/auth/logout');
  });
});
