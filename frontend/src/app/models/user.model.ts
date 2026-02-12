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