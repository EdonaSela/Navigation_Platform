import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from './services/auth.service';

@Component({
  selector: 'app-root',
  templateUrl: './app.html',
  styleUrls: ['./app.css'],
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class App {
  // logout(): void {
  //   fetch('http://localhost:5122/api/auth/logout', {
  //     method: 'POST',
  //     credentials: 'include'
  //   }).finally(() => {
  //     window.location.href = '/auth/login';
  //   });
  // }

  logout(): void {
  // Clear local storage first to satisfy "Clear client-side tokens"
  localStorage.clear();
  sessionStorage.clear();

  this.redirectTo('/api/auth/logout');
}

  public readonly authService = inject(AuthService);

  private redirectTo(url: string): void {
    window.location.assign(url);
  }
}
