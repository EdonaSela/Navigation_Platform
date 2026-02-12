import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class JourneyService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = '/api/journeys';
  private readonly apiUrlAdmin = '/api/admin';


  // createJourney(journeyData: CreateJourneyRequest): Observable<CreateJourneyResponse> {
  //   return this.http.post<CreateJourneyResponse>(this.apiUrl, journeyData);
  // }

  createJourney(journeyData: CreateJourneyRequest): Observable<CreateJourneyResponse> {
  return this.http.post<CreateJourneyResponse>(this.apiUrl, journeyData, { 
    withCredentials: true 
  });
}

  getJourneys(page: number = 1, pageSize: number = 20): Observable<Journey[]> {
    return this.http.get<Journey[]>(`${this.apiUrl}?Page=${page}&PageSize=${pageSize}`);
  }

  getJourneyById(id: string): Observable<Journey> {
    return this.http.get<Journey>(`${this.apiUrl}/${id}`,{ 
    withCredentials: true 
  });
  }

  updateJourney(id: string, journeyData: UpdateJourneyRequest): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/${id}`, journeyData,{ 
    withCredentials: true 
  });
  }

  deleteJourney(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`,{ 
    withCredentials: true 
  });
  }


  shareJourney(id: string, emails: string[]): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/${id}/share`, { emails });
  }

  // Gjenerim linku
  generatePublicLink(id: string): Observable<{ url: string }> {
    return this.http.post<{ url: string }>(`${this.apiUrl}/${id}/public-link`, {});
  }

  //Revoke
  revokePublicLink(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}/public-link`);
  }

  getPublicJourney(token: string): Observable<any> {
 
  return this.http.get<any>(`${this.apiUrl}/shared/${token}`);
}

favoriteJourney(journeyId: string): Observable<void> {

  return this.http.post<void>(`${this.apiUrl}/${journeyId}/favorite`, {});
}

unfavoriteJourney(journeyId: string): Observable<void> {
  return this.http.delete<void>(`${this.apiUrl}/${journeyId}/favorite`);
}
getAdminJourneys(params: any) {
  const cleanParams = Object.keys(params)
    .filter(k => params[k] !== null && params[k] !== undefined && params[k] !== '')
    .reduce((a, k) => ({ ...a, [k]: params[k] }), {});

  return this.http.get<Journey[]>(`${this.apiUrlAdmin}/journeys`, {
    params: cleanParams, 
    observe: 'response',
    withCredentials: true
  });
}

getPublicJourneys(page: number = 1, pageSize: number = 20): Observable<Journey[]> {
  return this.http.get<Journey[]>(`${this.apiUrl}/public?Page=${page}&PageSize=${pageSize}`);
}

getMonthlyStats1(): Observable<MonthlyDistanceDto[]> {
  return this.http.get<MonthlyDistanceDto[]>(`${this.apiUrlAdmin}/statistics/monthly-distance`);
}

getMonthlyStats(): Observable<PagedResult<MonthlyDistanceDto>> {
  return this.http.get<PagedResult<MonthlyDistanceDto>>(`${this.apiUrlAdmin}/statistics/monthly-distance`);
}



}
export enum UserStatus {
  Active = 'Active',
  Suspended = 'Suspended',
  Deactivated = 'Deactivated'
}

export interface UserProfile {
  id: string;
  email: string;
  status: UserStatus;
  createdAt: Date;
}

export interface MonthlyDistanceDto {
  year: number; 
  month: number; 
  totalDistanceKm: number; 
}

export interface PagedResult<T> {
  items: T[];
  pageNumber: number;
  totalPages: number;
  totalCount: number;
}
export interface CreateJourneyRequest {
  startLocation: string;
  startTime: string;
  arrivalLocation: string;
  arrivalTime: string;
  distanceKm: number;
  transportType: string;
}

export interface UpdateJourneyRequest extends CreateJourneyRequest {
  id: string;
}

export interface CreateJourneyResponse {
  id: string;
}

export interface JourneyFavorite {
  id: string;
  journeyId: string;
  userId: string;
}

export interface Journey {
  id: string;
  userId:string;
  startLocation: string;
  startTime: string;
  arrivalLocation: string;
  arrivalTime: string;
  distanceKm: number;
  transportType: string;
  isDailyGoalAchieved?: boolean;
  publicSharingToken: string;
  favorites: JourneyFavorite[];
}

export interface PagedJourneys {
  items: Journey[];
  page: number;
  pageSize: number;
  totalCount: number;
}
