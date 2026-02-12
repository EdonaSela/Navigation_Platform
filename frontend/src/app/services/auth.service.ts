import { Injectable, computed, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { jwtDecode } from 'jwt-decode';

import { OidcSecurityService } from 'angular-auth-oidc-client'; // Or your specific OIDC lib
@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = '/api/auth';
  private token = signal<string | null>(localStorage.getItem('access_token'));
  public readonly userData = signal<any>(null);
  register(payload: RegisterRequest): Observable<unknown> {
    return this.http.post(`${this.apiUrl}/register`, payload);
  }

  login(payload: LoginRequest): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(`${this.apiUrl}/login`, payload);
  }
private userIdSignal = signal<string | null>(null);
constructor() {
    this.fetchCurrentUser();
    
  }

  // auth.service.ts
logout() {
  localStorage.clear();
  sessionStorage.clear();
  window.location.href = `${this.apiUrl}/logout`;
}
 getUserId(): string | null {
    return this.userIdSignal();
  }
readonly isAdmin = computed(() => {
  const user = this.userData();
  if (!user) return false;

  const hasAdminRole = Array.isArray(user.roles)
    ? user.roles.includes('Admin')
    : user.roles === 'Admin';

  const hasAdminScope = Array.isArray(user.scopes)
    ? user.scopes.some((s: string) => s.toLowerCase() === 'admin')
    : false;

  return hasAdminRole || hasAdminScope;
});


 private fetchCurrentUser(): void {
  // 1. Change the GET type to 'any' to receive the full object
  this.http.get<any>(`${this.apiUrl}/me`).subscribe({
    next: (user) => {
      console.log('User data from /me:', user);
      
      this.userData.set(user); 
      
      this.userIdSignal.set(user.id ?? null);
    },
    error: () => {
      this.userData.set(null);
      this.userIdSignal.set(null);
      console.warn('User is not logged in.');
    }
  });
}


}

export interface RegisterRequest {
  email: string;
  password: string;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface LoginResponse {
  message?: string;
  token?: string;
}
