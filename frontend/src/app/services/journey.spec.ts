import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';

import { JourneyService } from './journey.service'; // Adjust this to your actual filename!
describe('Journey', () => {
  let service: JourneyService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.resetTestingModule();

    TestBed.configureTestingModule({
      providers: [
        JourneyService,
        provideHttpClient(),
        provideHttpClientTesting()
      ]
    });
    service = TestBed.inject(JourneyService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should call getJourneys with paging parameters', () => {
    service.getJourneys(2, 15).subscribe();

    const req = httpMock.expectOne('/api/journeys?Page=2&PageSize=15');
    expect(req.request.method).toBe('GET');
    req.flush([]);
  });

  it('should call createJourney with credentials', () => {
    service.createJourney({
      startLocation: 'A',
      startTime: '2026-01-01T10:00:00Z',
      arrivalLocation: 'B',
      arrivalTime: '2026-01-01T11:00:00Z',
      distanceKm: 10,
      transportType: 'Car'
    }).subscribe();

    const req = httpMock.expectOne('/api/journeys');
    expect(req.request.method).toBe('POST');
    expect(req.request.withCredentials).toBe(true);
    req.flush({ id: 'journey-1' });
  });

  it('should call getJourneyById with credentials', () => {
    service.getJourneyById('journey-1').subscribe();

    const req = httpMock.expectOne('/api/journeys/journey-1');
    expect(req.request.method).toBe('GET');
    expect(req.request.withCredentials).toBe(true);
    req.flush({});
  });

  it('should call updateJourney with credentials', () => {
    const payload = {
      id: 'journey-1',
      startLocation: 'A',
      startTime: '2026-01-01T10:00:00Z',
      arrivalLocation: 'B',
      arrivalTime: '2026-01-01T11:00:00Z',
      distanceKm: 10,
      transportType: 'Car'
    };

    service.updateJourney('journey-1', payload).subscribe();

    const req = httpMock.expectOne('/api/journeys/journey-1');
    expect(req.request.method).toBe('PUT');
    expect(req.request.withCredentials).toBe(true);
    req.flush({});
  });

  it('should call deleteJourney with credentials', () => {
    service.deleteJourney('journey-1').subscribe();

    const req = httpMock.expectOne('/api/journeys/journey-1');
    expect(req.request.method).toBe('DELETE');
    expect(req.request.withCredentials).toBe(true);
    req.flush({});
  });

  it('should clean null and empty params in getAdminJourneys', () => {
    service.getAdminJourneys({
      page: 1,
      pageSize: 20,
      orderBy: '',
      status: null
    }).subscribe();

    const req = httpMock.expectOne((request) =>
      request.url === '/api/admin/journeys' &&
      request.params.get('page') === '1' &&
      request.params.get('pageSize') === '20' &&
      !request.params.has('orderBy') &&
      !request.params.has('status'));

    expect(req.request.method).toBe('GET');
    expect(req.request.withCredentials).toBe(true);
    req.flush([]);
  });

  it('should call shareJourney', () => {
    service.shareJourney('journey-1', ['a@test.com']).subscribe();

    const req = httpMock.expectOne('/api/journeys/journey-1/share');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ emails: ['a@test.com'] });
    req.flush({});
  });

  it('should call generatePublicLink', () => {
    service.generatePublicLink('journey-1').subscribe();

    const req = httpMock.expectOne('/api/journeys/journey-1/public-link');
    expect(req.request.method).toBe('POST');
    req.flush({ url: 'http://x' });
  });

  it('should call revokePublicLink', () => {
    service.revokePublicLink('journey-1').subscribe();

    const req = httpMock.expectOne('/api/journeys/journey-1/public-link');
    expect(req.request.method).toBe('DELETE');
    req.flush({});
  });

  it('should call getPublicJourney', () => {
    service.getPublicJourney('token-1').subscribe();

    const req = httpMock.expectOne('/api/journeys/shared/token-1');
    expect(req.request.method).toBe('GET');
    req.flush({});
  });

  it('should call favorite and unfavorite', () => {
    service.favoriteJourney('journey-1').subscribe();
    const favReq = httpMock.expectOne('/api/journeys/journey-1/favorite');
    expect(favReq.request.method).toBe('POST');
    favReq.flush({});

    service.unfavoriteJourney('journey-1').subscribe();
    const unfavReq = httpMock.expectOne('/api/journeys/journey-1/favorite');
    expect(unfavReq.request.method).toBe('DELETE');
    unfavReq.flush({});
  });

  it('should call getPublicJourneys', () => {
    service.getPublicJourneys(1, 20).subscribe();

    const req = httpMock.expectOne('/api/journeys/public?Page=1&PageSize=20');
    expect(req.request.method).toBe('GET');
    req.flush([]);
  });

  it('should call monthly stats endpoints', () => {
    service.getMonthlyStats1().subscribe();
    const req1 = httpMock.expectOne('/api/admin/statistics/monthly-distance');
    expect(req1.request.method).toBe('GET');
    req1.flush([]);

    service.getMonthlyStats().subscribe();
    const req2 = httpMock.expectOne('/api/admin/statistics/monthly-distance');
    expect(req2.request.method).toBe('GET');
    req2.flush({ items: [], pageNumber: 1, totalPages: 1, totalCount: 0 });
  });
});
