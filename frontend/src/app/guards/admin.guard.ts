import { HttpClient } from '@angular/common/http';
import { inject } from '@angular/core';
import { CanActivateFn, Router, UrlTree } from '@angular/router';
import { catchError, map, of } from 'rxjs';
import { AuthService } from '../services/auth.service';

const AUTH_ME_URL = '/api/auth/me';

export const adminGuard: CanActivateFn = () => {
  const authService = inject(AuthService);
  const router = inject(Router);
  const http = inject(HttpClient);

  if (authService.userData()) {
    return authService.isAdmin() ? true : router.parseUrl('/journeys');
  }

  return http.get<any>(AUTH_ME_URL, { withCredentials: true }).pipe(
    map((user) => {
      authService.userData.set(user);
      return authService.isAdmin() ? true : router.parseUrl('/journeys');
    }),
    catchError(() => of<UrlTree>(router.parseUrl('/auth/login')))
  );
};
