import { ChangeDetectionStrategy, Component, InjectionToken, inject, signal } from '@angular/core';

const WINDOW = new InjectionToken<Window>('Window', {
  factory: () => window
});
const API_BASE_URL = '/api';

@Component({
  selector: 'app-login',
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.css'],
  imports: [],
  providers: [{ provide: WINDOW, useFactory: () => window }],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class LoginComponent {
  private readonly windowRef = inject(WINDOW);
  readonly isRedirecting = signal(false);

  redirectToAzureLogin(): void {
    this.isRedirecting.set(true);
    const returnUrl = encodeURIComponent(`${this.windowRef.location.origin}/journeys`);
    this.windowRef.location.href = `${API_BASE_URL}/auth/login?returnUrl=${returnUrl}`;
  }
}
