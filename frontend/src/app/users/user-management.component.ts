// src/app/features/admin/user-management.component.ts
import { ChangeDetectorRef, Component, inject, OnInit } from '@angular/core';
import { AdminService } from '../services/admin.service';
import { UserProfile, UserStatus } from '../models/user.model';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-user-management',
  templateUrl: './user-management.component.html',
   styleUrls: ['./user-management.component.css'],
  imports: [CommonModule]
})
export class UserManagementComponent implements OnInit {
  users: UserProfile[] = [];
  statuses = Object.values(UserStatus);
 private cdr = inject(ChangeDetectorRef);
  constructor(private adminService: AdminService) {}

  ngOnInit() {
    this.loadUsers();
  }

  loadUsers() {
   this.adminService.getUsers().subscribe({
    next: (data) => {
      console.log("Raw data from backend:");
      console.table(data); // This will show you the exact property names
      this.users = data;
      this.cdr.detectChanges();
    },
    error: (err) => console.error("Could not load users", err)
  });
  }

  onStatusChange(user: UserProfile, newStatus: any) {
    const status = newStatus.target.value as UserStatus;
    
    this.adminService.updateStatus(user.id, status).subscribe({
      next: () => {
        user.status = status;
        alert('Status updated successfully and audit log recorded.');
      },
      error: (err) => alert('Failed to update: ' + err.error.message)
    });
  }
}