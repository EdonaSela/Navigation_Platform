import { HttpRequest, HttpResponse } from '@angular/common/http';
import { firstValueFrom, of } from 'rxjs';
import { withCredentialsInterceptor } from './with-credentials.interceptor';

describe('withCredentialsInterceptor', () => {
  it('should set withCredentials to true', async () => {
    const request = new HttpRequest('GET', '/api/test');

    await firstValueFrom(withCredentialsInterceptor(request, (nextRequest) => {
      expect(nextRequest.withCredentials).toBe(true);
      return of(new HttpResponse({ status: 200 }));
    }));
  });
});
