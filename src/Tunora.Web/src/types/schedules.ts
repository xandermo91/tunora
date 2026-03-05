export interface Schedule {
  id: number;
  name: string;
  instanceId: number;
  channelId: number;
  channelName: string;
  channelAccentColor: string;
  daysOfWeek: number[]; // 0=Sunday … 6=Saturday
  startTime: string;    // "HH:mm"
  endTime: string;      // "HH:mm"
  isActive: boolean;
  createdAt: string;
}

export interface CreateScheduleRequest {
  name: string;
  channelId: number;
  daysOfWeek: number[];
  startTime: string;
  endTime: string;
}

export interface UpdateScheduleRequest extends CreateScheduleRequest {
  isActive: boolean;
}
