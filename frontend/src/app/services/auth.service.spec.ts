import { TestBed } from '@angular/core/testing';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { vi } from 'vitest';
import { AuthService } from './auth.service';

describe('AuthService', () => {
  let service: AuthService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.resetTestingModule();
    TestBed.configureTestingModule({
      providers: [AuthService, provideHttpClient(), provideHttpClientTesting()]
    });

    service = TestBed.inject(AuthService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should fetch current user on creation', () => {
    const req = httpMock.expectOne('/api/auth/me');
    expect(req.request.method).toBe('GET');
    req.flush({ id: 'user-1', roles: ['User'], scopes: [] });

    expect(service.getUserId()).toBe('user-1');
    expect(service.userData()?.id).toBe('user-1');
    expect(service.isAdmin()).toBe(false);
  });

  it('should set admin from roles', () => {
    const req = httpMock.expectOne('/api/auth/me');
    req.flush({ id: 'admin-1', roles: ['Admin'], scopes: [] });

    expect(service.isAdmin()).toBe(true);
  });

  it('should set admin from scopes', () => {
    const req = httpMock.expectOne('/api/auth/me');
    req.flush({ id: 'admin-2', roles: [], scopes: ['profile', 'ADMIN'] });

    expect(service.isAdmin()).toBe(true);
  });

  it('should handle /me error as logged out', () => {
    const req = httpMock.expectOne('/api/auth/me');
    req.flush({}, { status: 401, statusText: 'Unauthorized' });

    expect(service.getUserId()).toBeNull();
    expect(service.userData()).toBeNull();
    expect(service.isAdmin()).toBe(false);
  });

  it('should post register', () => {
    httpMock.expectOne('/api/auth/me').flush({ id: 'u' });

    service.register({ email: 'a@a.com', password: 'pass' }).subscribe();
    const req = httpMock.expectOne('/api/auth/register');
    expect(req.request.method).toBe('POST');
    req.flush({});
  });

  it('should post login', () => {
    httpMock.expectOne('/api/auth/me').flush({ id: 'u' });

    service.login({ email: 'a@a.com', password: 'pass' }).subscribe();
    const req = httpMock.expectOne('/api/auth/login');
    expect(req.request.method).toBe('POST');
    req.flush({ token: 'abc' });
  });

  it('should clear storage and redirect on logout', () => {
    httpMock.expectOne('/api/auth/me').flush({ id: 'u' });
    const clearSpy = vi.spyOn(Storage.prototype, 'clear');
    const redirectSpy = vi.spyOn(service as any, 'redirectTo');

    service.logout();

    expect(clearSpy).toHaveBeenCalledTimes(2);
    expect(redirectSpy).toHaveBeenCalledWith('/api/auth/logout');
  });
});
