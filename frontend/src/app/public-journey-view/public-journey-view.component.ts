import { Component, OnInit, signal, inject } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { JourneyService, Journey, UpdateJourneyRequest } from '../services/journey.service';


import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-public-journey-view',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './public-journey-view.component.html'
})
export class PublicJourneyViewComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private journeyService = inject(JourneyService);

  // Signals to hold state
  journey = signal<any>(null);
  errorMessage = signal<string | null>(null);

  ngOnInit(): void {
    const token = this.route.snapshot.paramMap.get('token');
    
    if (token) {
      this.journeyService.getPublicJourney(token).subscribe({
        next: (data) => {
          this.journey.set(data);
          this.errorMessage.set(null);
        },
        error: (err) => {
          if (err.status === 410) {
            this.errorMessage.set("This link has been revoked by the owner."); 
          } else {
            this.errorMessage.set("Journey not found or link expired.");
          }
        }
      });
    }
  }
}