import { ChangeDetectorRef, Component, effect, inject, OnInit } from "@angular/core";
import { JourneyService, MonthlyDistanceDto,PagedResult } from "../services/journey.service";
import { SignalRService } from "../services/signalR.service";
import { CommonModule } from "@angular/common";


@Component({
  selector: 'monthly-stat',
  templateUrl: './monthly-stats.component.html',
  styleUrls: ['./monthly-stats.component.css'],
  standalone: true, 
  imports: [CommonModule]
 
})
export class MonthlyStatsComponent implements OnInit {

  
    private signalRService = inject(SignalRService);
  private journeyService = inject(JourneyService);
  private cdr = inject(ChangeDetectorRef);
  
  //stats: MonthlyDistanceDto[] = [];

stats: PagedResult<MonthlyDistanceDto> | null = null;

private statsUpdater = effect(() => {
    const trigger = this.signalRService.statsChangedSignal();
   
    if (trigger) {
      console.log('SignalR Triggered! Reloading stats...');
      setTimeout(() => {
      this.loadStats();
    }, 1000);
    }
  });
constructor() {
    
    // effect(() => {
    //   this.signalRService.statsChangedSignal(); 
    //   this.loadStats(); 
    // });
  }

  ngOnInit() {
    debugger;
    this.loadStats();

    
  }

loadStats() {
  this.journeyService.getMonthlyStats().subscribe({
    next: (data) => {
      console.log('Data received:', data);
     this.stats = { 
        items: [...data.items], // Create a new array reference
        pageNumber: data.pageNumber,
        totalCount: data.totalCount,
        totalPages: data.totalPages
      };
      
    
      this.cdr.detectChanges();
    },
    error: (err) => console.error('Error loading stats', err)
  });
}

  getMonthName(monthNumber: number): string {
    const date = new Date();
    date.setMonth(monthNumber - 1);
    return date.toLocaleString('default', { month: 'long' }); 
  }
}