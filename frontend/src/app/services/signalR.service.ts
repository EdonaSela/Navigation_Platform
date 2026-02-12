import { Injectable, signal } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { Journey } from './journey.service';

@Injectable({ providedIn: 'root' })
export class SignalRService {
  private hubConnection: signalR.HubConnection;
  
  public newJourneySignal = signal<any>(null);
  public deletedJourneyIdSignal = signal<string | null>(null);
  public updatedJourneySignal = signal<Journey | null>(null);
   public statsChangedSignal = signal<number>(0);
  constructor() {
    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl('/hubs/journey', {
    accessTokenFactory: () => localStorage.getItem('token') ?? ''
  })
      .withAutomaticReconnect()
      .build();

    this.startConnection();

    
  }

  private startConnection() {

  this.registerHandlers(); 

  this.hubConnection
    .start()
    .then(() => console.log('SignalR Connected'))
    .catch(err => console.error('SignalR Error: ', err));
}

private registerHandlers() {
  this.hubConnection.on('JourneyCreated', (data: Journey) => {
    this.newJourneySignal.set(data);
    this.statsChangedSignal.update(n => n + 1);
  });

  this.hubConnection.on('JourneyDeleted', (id: string) => {
    this.deletedJourneyIdSignal.set(id);
    this.statsChangedSignal.update(n => n + 1);

  });

  this.hubConnection.on('JourneyUpdated', (journey: Journey) => {
    this.updatedJourneySignal.set(journey);
    this.statsChangedSignal.update(n => n + 1);
  });


  this.hubConnection.on('DailyGoalAchieved', (journey: Journey) => {
    this.updatedJourneySignal.set(journey);
  });
}

//   private startConnection() {
//     this.hubConnection
//       .start()
//      .then(() => {
//       console.log('SignalR Connected');
//       this.registerHandlers(); 
//     })
//       .catch(err => console.error('SignalR Error: ', err));

    
//     this.hubConnection.on('JourneyCreated', (data) => {
//       this.newJourneySignal.set(data);
//     });
//   }

//   private registerHandlers() {
//   this.hubConnection.on('JourneyDeleted', (id: string) => {
//     this.deletedJourneyIdSignal.set(id);
//   });

//   this.hubConnection.on('JourneyUpdated', (journey: Journey) => {
//     this.updatedJourneySignal.set(journey);
//   });
  
  
//   this.hubConnection.on('DailyGoalAchieved', (journey: Journey) => {
//     this.updatedJourneySignal.set(journey);
//   });
// }
}
