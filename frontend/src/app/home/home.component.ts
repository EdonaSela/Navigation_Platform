import { 
  ChangeDetectionStrategy, Component, DestroyRef, computed, inject, signal, 
  OnInit, effect // Added effect and OnInit
} from '@angular/core';
import { ReactiveFormsModule, Validators, NonNullableFormBuilder } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { finalize } from 'rxjs';
import { JourneyService, CreateJourneyRequest, Journey } from '../services/journey.service';
import { SignalRService } from '../services/signalR.service'; // Import your SignalR service
@Component({
  selector: 'app-home',
  templateUrl: './home.component.html',
  styleUrls: ['./home.component.css'],
  imports: [ReactiveFormsModule, RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush
})


export class HomeComponent implements OnInit{
  private readonly journeyService = inject(JourneyService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly fb = inject(NonNullableFormBuilder);
   private readonly signalRService = inject(SignalRService); 
  readonly isSubmitting = signal(false);
  readonly isLoading = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly successMessage = signal<string | null>(null);
  readonly journeys = signal<Journey[]>([]);
  readonly page = signal(1);
  readonly pageSize = signal(10);
  readonly totalCount = signal(0);
  readonly totalPages = computed(() => Math.max(1, Math.ceil(this.totalCount() / this.pageSize())));

  readonly form = this.fb.group({
    startLocation: ['', [Validators.required, Validators.minLength(2)]],
    startTime: ['', [Validators.required]],
    arrivalLocation: ['', [Validators.required, Validators.minLength(2)]],
    arrivalTime: ['', [Validators.required]],
    distanceKm: [0, [Validators.required, Validators.min(0.01)]],
    transportType: ['Car', [Validators.required]]
  });
constructor() {
  effect(() => {
    // 1. Handle New Journey (Already implemented)
    const newJourney = this.signalRService.newJourneySignal();
    if (newJourney) {
      this.journeys.update(current => [newJourney, ...current]);
      this.totalCount.update(count => count + 1);
    }

    // 2. Handle Deleted Journey
    const deletedId = this.signalRService.deletedJourneyIdSignal(); // You'll need this in SignalRService
    if (deletedId) {
      this.journeys.update(current => current.filter(j => j.id !== deletedId));
      this.totalCount.update(count => Math.max(0, count - 1));
    }

    // 3. Handle Updated Journey (e.g., Goal Achieved status)
    const updatedJourney = this.signalRService.updatedJourneySignal();
    if (updatedJourney) {
      this.journeys.update(current => 
        current.map(j => j.id === updatedJourney.id ? updatedJourney : j)
      );
    }
  });
}
  ngOnInit(): void {
    this.loadJourneys();
  }



  loadJourneys(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.journeyService
      .getJourneys(this.page(), this.pageSize())
      .pipe(
        finalize(() => this.isLoading.set(false)),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: (result: Journey[]) => {
          this.journeys.set(result);
          this.totalCount.set(result.length);
        },
        error: () => {
          this.errorMessage.set('Failed to load journeys.');
        }
      });
  }

  nextPage(): void {
    if (this.page() >= this.totalPages()) {
      return;
    }
    this.page.update((value) => value + 1);
    this.loadJourneys();
  }

  previousPage(): void {
    if (this.page() <= 1) {
      return;
    }
    this.page.update((value) => value - 1);
    this.loadJourneys();
  }

  deleteJourney(id: string): void {
    this.errorMessage.set(null);
    this.journeyService
      .deleteJourney(id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.successMessage.set('Journey deleted.');
          //this.loadJourneys();
        },
        error: () => {
          this.errorMessage.set('Failed to delete journey.');
        }
      });
  }

  submit(): void {
    debugger;
    this.errorMessage.set(null);
    this.successMessage.set(null);

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const raw = this.form.getRawValue();
    const payload: CreateJourneyRequest = {
      ...raw,
      startTime: this.toIsoString(raw.startTime),
      arrivalTime: this.toIsoString(raw.arrivalTime)
    };
    this.isSubmitting.set(true);

    this.journeyService
      .createJourney(payload)
      .pipe(finalize(() => this.isSubmitting.set(false)))
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.successMessage.set('Journey created.');
          this.form.reset({
            startLocation: '',
            startTime: '',
            arrivalLocation: '',
            arrivalTime: '',
            distanceKm: 0,
            transportType: 'Car'
          });
          this.loadJourneys();
        },
        error: () => {
          this.errorMessage.set('Failed to create journey.');
        }
      });
  }

  private toIsoString(value: string): string {
    const parsed = new Date(value);
    return parsed.toISOString();
  }
}
