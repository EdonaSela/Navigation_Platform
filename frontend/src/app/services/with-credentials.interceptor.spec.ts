import { HttpRequest, HttpResponse } from '@angular/common/http';
import { of } from 'rxjs';
import { withCredentialsInterceptor } from './with-credentials.interceptor';

describe('withCredentialsInterceptor', () => {
  it('should set withCredentials to true', (done) => {
    const request = new HttpRequest('GET', '/api/test');

    withCredentialsInterceptor(request, (nextRequest) => {
      expect(nextRequest.withCredentials).toBe(true);
      done();
      return of(new HttpResponse({ status: 200 }));
    });
  });
});
