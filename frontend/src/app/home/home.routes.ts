import { Routes } from '@angular/router';

export const homeRoutes: Routes = [
  {
    path: '',
    loadComponent: () => import('./home.component').then((m) => m.HomeComponent)
  },
  {
    path: ':id',
    loadComponent: () =>
      import('../journey-detail/journey-detail.component').then((m) => m.JourneyDetailComponent)
  }
];
