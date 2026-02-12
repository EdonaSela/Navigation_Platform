import { Component, signal, inject, ChangeDetectionStrategy, effect } from '@angular/core';
import { JourneyService, Journey } from '../services/journey.service';
import { RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
import { NonNullableFormBuilder, ReactiveFormsModule } from '@angular/forms';
import { AuthService } from '../services/auth.service';
import { SignalRService } from '../services/signalR.service';

@Component({
  selector: 'app-admin-journeys',
  templateUrl: './admin-journeys.component.html',
  styleUrls: ['./admin-journeys.component.css'],
  standalone: true,
  imports: [ReactiveFormsModule, CommonModule,RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AdminJourneysComponent {
  private journeyService = inject(JourneyService);
  private fb = inject(NonNullableFormBuilder);
  private signalRService = inject(SignalRService);
  public readonly authService = inject(AuthService);
  journeys = signal<Journey[]>([]);
  totalCount = signal(0);
  pageSize = 10;
  currentPage = 1;

  // Filtering Form
  filterForm = this.fb.group({
    userId: [''],
    transportType: [''],
    minDistance: [null as number | null],
    maxDistance: [null as number | null],
    startDateFrom: [''],
    startDateTo: [''],
    arrivalDateFrom: [''],
    arrivalDateTo: ['']
  });

  constructor() {
    effect(() => {
      // Re-query current admin page whenever a real-time journey event arrives.
      this.signalRService.statsChangedSignal();
      this.loadAdminData();
    });
  }

  loadAdminData() {
    const formValue = this.filterForm.getRawValue();
    const filters = {
      UserId: formValue.userId,
      TransportType: formValue.transportType,
      StartDateFrom: formValue.startDateFrom,
      StartDateTo: formValue.startDateTo,
      ArrivalDateFrom: formValue.arrivalDateFrom,
      ArrivalDateTo: formValue.arrivalDateTo,
      MinDistance: formValue.minDistance,
      MaxDistance: formValue.maxDistance,
      Page: this.currentPage,
      PageSize: this.pageSize,
      OrderBy: 'StartTime',
      Direction: 'desc'
    };

    this.journeyService.getAdminJourneys(filters).subscribe(res => {
      this.journeys.set(res.body ?? []);
      const count = res.headers.get('X-Total-Count');
      this.totalCount.set(Number(count) || 0);
    });
  }

  applyFilters() {
    this.currentPage = 1;
    this.loadAdminData();
  }
}
