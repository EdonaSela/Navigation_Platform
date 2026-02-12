import { Routes } from '@angular/router';
import { PublicJourneyViewComponent } from './public-journey-view/public-journey-view.component';
import { PublicJourneysComponent } from './public-journeys/public-journeys.component';
import { MonthlyStatsComponent } from './admin-monthly-stats/monthly-stats.component';
import { UserManagementComponent } from './users/user-management.component';
import { adminGuard } from './guards/admin.guard';

export const routes: Routes = [
  {
    path: '',
    pathMatch: 'full',
    redirectTo: 'auth/login'
  },
  {
    path: 'journeys',
    loadChildren: () => import('./home/home.routes').then((m) => m.homeRoutes)
  },
  {
    path: 'home/journeys',
    redirectTo: 'journeys'
  },
  {
    path: 'home',
    pathMatch: 'full',
    redirectTo: 'journeys'
  },
  {
    path: 'auth',
    loadChildren: () => import('./auth/auth.routes').then((m) => m.authRoutes)
  },
  // {
  //   path: 'api/journeys/shared/:token', 
  //   component: PublicJourneyViewComponent
  // },

  { 
  path: 'shared/:token', 
  component: PublicJourneyViewComponent // A new component for anonymous users
  },
  {
    path: 'public-journeys',
    component: PublicJourneysComponent
  },

  { 
    path: 'admin/journeys', 
    loadComponent: () => import('./admin/admin-journeys.component').then(m => m.AdminJourneysComponent),
    canActivate: [adminGuard]
  },
  { path: 'monthly-stats', component: MonthlyStatsComponent, canActivate: [adminGuard] },
  { path: 'users', component: UserManagementComponent, canActivate: [adminGuard] },

  {
    path: '**',
    redirectTo: ''
  }
];
