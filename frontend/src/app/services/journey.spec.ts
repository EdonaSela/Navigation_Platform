import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';

import { JourneyService } from './journey.service'; // Adjust this to your actual filename!
describe('Journey', () => {
  let service: JourneyService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
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

    const req = httpMock.expectOne('http://localhost:5122/api/journeys?Page=2&PageSize=15');
    expect(req.request.method).toBe('GET');
    req.flush([]);
  });

  it('should call getJourneyById with credentials', () => {
    service.getJourneyById('journey-1').subscribe();

    const req = httpMock.expectOne('http://localhost:5122/api/journeys/journey-1');
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

    const req = httpMock.expectOne('http://localhost:5122/api/journeys/journey-1');
    expect(req.request.method).toBe('PUT');
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
      request.url === 'http://localhost:5122/api/admin/journeys' &&
      request.params.get('page') === '1' &&
      request.params.get('pageSize') === '20' &&
      !request.params.has('orderBy') &&
      !request.params.has('status'));

    expect(req.request.method).toBe('GET');
    req.flush([]);
  });
});
