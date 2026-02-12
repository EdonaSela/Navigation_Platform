import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal,computed, effect } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { ReactiveFormsModule, Validators, NonNullableFormBuilder } from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { finalize } from 'rxjs';
import { JourneyService, Journey, UpdateJourneyRequest } from '../services/journey.service';
import { AuthService } from '../services/auth.service'; 
import { SignalRService } from '../services/signalR.service'; 
@Component({
  selector: 'app-journey-detail',
  templateUrl: './journey-detail.component.html',
  styleUrls: ['./journey-detail.component.css'],
  imports: [ReactiveFormsModule, RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class JourneyDetailComponent {
  private readonly journeyService = inject(JourneyService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);
  private authService = inject(AuthService);
  private readonly fb = inject(NonNullableFormBuilder);
  private readonly signalrService = inject(SignalRService);
  readonly journey = signal<Journey | null>(null);
  readonly isLoading = signal(false);
  readonly isSaving = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly successMessage = signal<string | null>(null);
  
  publicUrl = signal<string | null>(null);
  isFavoriting = signal(false);

  readonly totalFavorites = computed(() => {
  return this.journey()?.favorites?.length ?? 0;
});

readonly isOwner = computed(() => {
  debugger;
  return this.journey()?.userId === this.currentUserId;
});
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
      debugger;
      const remoteUpdate = this.signalrService.updatedJourneySignal();
      const localJourney = this.journey();

      if (remoteUpdate && localJourney && remoteUpdate.id === localJourney.id) {
        console.log('Real-time sync: updating journey data from SignalR');
            console.log('SignalR favorites count:', remoteUpdate.favorites?.length);

        this.journey.set(remoteUpdate);
        
        this.form.patchValue({
            startLocation: remoteUpdate.startLocation,
            startTime: this.fromIsoString(remoteUpdate.startTime),
            arrivalLocation: remoteUpdate.arrivalLocation,
            arrivalTime: this.fromIsoString(remoteUpdate.arrivalTime),
            distanceKm: remoteUpdate.distanceKm,
            transportType: remoteUpdate.transportType
        }, { emitEvent: false });
      }
    });
  }

get currentUserId(): string | null {
  debugger;
  return this.authService.getUserId();
}




readonly isAlreadyFavorited = computed(() => {
  const journey = this.journey();
  const userId = this.currentUserId;
  debugger;
  
  if (!journey || !userId) return false;
  console.log('Comparing journey favorites with current user:', journey.favorites, userId);
 // return journey.favorites?.some(f => f.userId === userId) ?? false;
  return journey.favorites?.some(f => f.userId.toLowerCase() === userId.toLowerCase()) ?? false;

  
});

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) {
      this.errorMessage.set('Journey not found.');
      return;
    }
    this.loadJourney(id);

    


  }

  loadJourney(id: string): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.journeyService
      .getJourneyById(id)
      .pipe(finalize(() => this.isLoading.set(false)))
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (journey) => {
          this.journey.set(journey);
          this.form.reset({
            startLocation: journey.startLocation,
            startTime: this.fromIsoString(journey.startTime),
            arrivalLocation: journey.arrivalLocation,
            arrivalTime: this.fromIsoString(journey.arrivalTime),
            distanceKm: journey.distanceKm,
            transportType: journey.transportType
          });

      if (journey.publicSharingToken) {
          const url = `${window.location.origin}/api/journeys/shared/${journey.publicSharingToken}`;
          this.publicUrl.set(url);
        } else {
          this.publicUrl.set(null);
        }

        },
        error: () => {
          this.errorMessage.set('Failed to load journey.');
        }
      });
  }

  save(): void {
    if (!this.journey()) {
      return;
    }
    this.errorMessage.set(null);
    this.successMessage.set(null);

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const raw = this.form.getRawValue();
    const payload: UpdateJourneyRequest = {
      id: this.journey()!.id,
      startLocation: raw.startLocation,
      startTime: this.toIsoString(raw.startTime),
      arrivalLocation: raw.arrivalLocation,
      arrivalTime: this.toIsoString(raw.arrivalTime),
      distanceKm: raw.distanceKm,
      transportType: raw.transportType
    };

    this.isSaving.set(true);
    this.journeyService
      .updateJourney(payload.id, payload)
      .pipe(finalize(() => this.isSaving.set(false)))
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.successMessage.set('Journey updated.');
        },
        error: () => {
          this.errorMessage.set('Failed to update journey.');
        }
      });
  }

  delete(): void {
    const current = this.journey();
    if (!current) {
      return;
    }
    this.journeyService
      .deleteJourney(current.id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.router.navigate(['/journeys']);
        },
        error: () => {
          this.errorMessage.set('Failed to delete journey.');
        }
      });
  }

  private toIsoString(value: string): string {
    return new Date(value).toISOString();
  }

  private fromIsoString(value: string): string {
    const date = new Date(value);
    const pad = (input: number) => input.toString().padStart(2, '0');
    return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(
      date.getHours()
    )}:${pad(date.getMinutes())}`;
  }

  shareWithUser(friendEmail: string) {
  const email = friendEmail?.trim();
  if (!email) return;
  this.journeyService.shareJourney(this.journey()!.id, [email]).subscribe({
    next: () => this.successMessage.set("Successfully shared!"),
    error: () => this.errorMessage.set("Failed to share.")
  });
}

generateLink() {
  debugger;
  const currentJourney = this.journey();
  if (!currentJourney) return;

  this.journeyService.generatePublicLink(currentJourney.id).subscribe({
    next: (res: { url: string }) => { 
      this.publicUrl.set(res.url);
      this.successMessage.set("Public link generated!");
    },
    error: () => this.errorMessage.set("Could not generate link.")
  });
}

revokeLink() {
  this.journeyService.revokePublicLink(this.journey()!.id).subscribe({
    next: () => {
      this.publicUrl.set(null);
      this.successMessage.set("Link revoked.");
    }
  });
}
toggleFavorite() {
  debugger;
  const currentJourney = this.journey();
  
  const currentUserId = this.authService.getUserId(); 
  
  if (!currentJourney || !currentUserId) return;

  this.isFavoriting.set(true);

  const isAlreadyFavorited = currentJourney.favorites?.some(f => f.userId === currentUserId);

  const request$ = isAlreadyFavorited
    ? this.journeyService.unfavoriteJourney(currentJourney.id)
    : this.journeyService.favoriteJourney(currentJourney.id);

    request$.subscribe({
  next: () => {
    this.isFavoriting.set(false);
    
    const currentJourney = this.journey();
    const userId = this.currentUserId;

    if (currentJourney && userId) {
   
      const updatedFavorites = isAlreadyFavorited
        ? currentJourney.favorites.filter(f => f.userId !== userId) 
        : [...(currentJourney.favorites ?? []), { id: '', journeyId: currentJourney.id, userId }]; 

   
      this.journey.set({
        ...currentJourney,
        favorites: updatedFavorites
      });
    }
  },
  error: (err: any) => {
    this.isFavoriting.set(false);
    console.error('Failed to update favorite status:', err);
  }
});

  // request$.subscribe({
  //   next: () => {
  //     this.isFavoriting.set(false);
    
  //   },
  //   error: (err: any) => { // Fixed the 'err' type error from your terminal
  //     this.isFavoriting.set(false);
  //     console.error(err);
  //   }
  // });
}

toggleFavorite1() {
    const currentJourney = this.journey();
    if (!currentJourney) return;

    this.isFavoriting.set(true);

    this.journeyService.favoriteJourney(currentJourney.id).subscribe({
      next: () => {
        this.isFavoriting.set(false);

        // this.successMessage.set('Added to favorites! You will receive updates.');
      },
      error: (err) => {
        this.isFavoriting.set(false);
        console.error(err);
      }
    });
  }
}
