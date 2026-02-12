import { ChangeDetectionStrategy, Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { finalize } from 'rxjs';
import { Journey, JourneyService } from '../services/journey.service';

@Component({
  selector: 'app-public-journeys',
  standalone: true,
  templateUrl: './public-journeys.component.html',
  styleUrls: ['./public-journeys.component.css'],
  imports: [RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class PublicJourneysComponent {
  private readonly journeyService = inject(JourneyService);
  private readonly destroyRef = inject(DestroyRef);

  readonly journeys = signal<Journey[]>([]);
  readonly isLoading = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly page = signal(1);
  readonly pageSize = signal(10);
  readonly totalCount = signal(0);
  readonly totalPages = computed(() => Math.max(1, Math.ceil(this.totalCount() / this.pageSize())));

  constructor() {
    this.loadPublicJourneys();
  }

  loadPublicJourneys(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.journeyService
      .getPublicJourneys(this.page(), this.pageSize())
      .pipe(
        finalize(() => this.isLoading.set(false)),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: (result) => {
          this.journeys.set(result);
          this.totalCount.set(result.length);
        },
        error: () => {
          this.errorMessage.set('Failed to load public journeys.');
        }
      });
  }

  nextPage(): void {
    if (this.page() >= this.totalPages()) {
      return;
    }

    this.page.update((value) => value + 1);
    this.loadPublicJourneys();
  }

  previousPage(): void {
    if (this.page() <= 1) {
      return;
    }

    this.page.update((value) => value - 1);
    this.loadPublicJourneys();
  }
}
