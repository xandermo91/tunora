export interface Instance {
  id: number;
  name: string;
  location: string;
  status: 'Offline' | 'Online' | 'Playing' | 'Stopped';
  activeChannelId: number | null;
  currentTrackTitle: string | null;
  currentTrackArtist: string | null;
  lastSeenAt: string | null;
  createdAt: string;
  channels: AssignedChannel[];
}

/** Returned only from POST /instances — includes the one-time ConnectionKey. */
export interface InstanceCreated extends Omit<Instance, 'activeChannelId' | 'currentTrackTitle' | 'currentTrackArtist' | 'lastSeenAt'> {
  connectionKey: string;
}

export interface AssignedChannel {
  channelId: number;
  name: string;
  iconName: string;
  accentColor: string;
  sortOrder: number;
}
