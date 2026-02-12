import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { UserProfile, UserStatus } from '../models/user.model';

@Injectable({ providedIn: 'root' })
export class AdminService {
  //private readonly apiUrl = '/api/admin/users';
    private readonly apiUrl = '/api/admin';


  constructor(private http: HttpClient) {}

  getUsers() {
    debugger;
   return this.http.get<UserProfile[]>(`${this.apiUrl}/users`);
  }

  updateStatus(userId: string, status: UserStatus) {
    return this.http.patch(`${this.apiUrl}/${userId}/status`, { status });
  }
}
