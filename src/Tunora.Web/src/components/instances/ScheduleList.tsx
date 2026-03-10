import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { schedulesApi } from '../../api/schedules';
import { channelsApi } from '../../api/channels';
import type { Schedule, CreateScheduleRequest, UpdateScheduleRequest } from '../../types/schedules';

const DAY_LABELS = ['Sun', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat'];
const HOUR_MARKERS = [0, 6, 12, 18];

interface Props {
  instanceId: number;
}

interface FormState {
  channelId: number;
  daysOfWeek: number[];
  startTime: string;
  endTime: string;
  isActive: boolean;
}

const defaultForm = (channelId: number): FormState => ({
  channelId,
  daysOfWeek: [0, 1, 2, 3, 4, 5, 6],
  startTime: '09:00',
  endTime: '17:00',
  isActive: true,
});

function toMinutes(time: string): number {
  const [h, m] = time.split(':').map(Number);
  return h * 60 + m;
}

interface Segment {
  left: number;
  width: number;
}

function getSegments(startTime: string, endTime: string): Segment[] {
  const start = toMinutes(startTime);
  const end = toMinutes(endTime);
  if (end <= start) {
    // Midnight-crossing: render as two segments
    return [
      { left: (start / 1440) * 100, width: ((1440 - start) / 1440) * 100 },
      { left: 0, width: (end / 1440) * 100 },
    ];
  }
  return [{ left: (start / 1440) * 100, width: ((end - start) / 1440) * 100 }];
}

function formatHourLabel(hour: number): string {
  if (hour === 0) return '12am';
  if (hour === 12) return '12pm';
  return hour < 12 ? `${hour}am` : `${hour - 12}pm`;
}

export default function ScheduleList({ instanceId }: Props) {
  const qc = useQueryClient();

  const { data: channels = [] } = useQuery({
    queryKey: ['channels'],
    queryFn: () => channelsApi.list().then(r => r.data),
    staleTime: Infinity,
  });

  const firstChannelId = channels[0]?.id ?? 0;

  const [selectedDay, setSelectedDay] = useState(() => new Date().getDay());
  const [editing, setEditing] = useState<Schedule | null>(null);
  const [showForm, setShowForm] = useState(false);
  const [form, setForm] = useState<FormState>(defaultForm(firstChannelId));
  const [apiError, setApiError] = useState<string | null>(null);

  const { data: schedules = [], isLoading } = useQuery({
    queryKey: ['schedules', instanceId],
    queryFn: () => schedulesApi.list(instanceId).then(r => r.data),
  });

  const invalidate = () => qc.invalidateQueries({ queryKey: ['schedules', instanceId] });

  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  const handleError = (err: any) => {
    setApiError(err?.response?.data?.error ?? 'An error occurred.');
  };

  const createMutation = useMutation({
    mutationFn: (data: CreateScheduleRequest) => schedulesApi.create(instanceId, data),
    onSuccess: () => { invalidate(); closeForm(); },
    onError: handleError,
  });

  const updateMutation = useMutation({
    mutationFn: ({ id, data }: { id: number; data: UpdateScheduleRequest }) =>
      schedulesApi.update(instanceId, id, data),
    onSuccess: () => { invalidate(); closeForm(); },
    onError: handleError,
  });

  const deleteMutation = useMutation({
    mutationFn: (id: number) => schedulesApi.delete(instanceId, id),
    onSuccess: invalidate,
  });

  const openAdd = () => {
    setEditing(null);
    setForm(defaultForm(firstChannelId));
    setApiError(null);
    setShowForm(true);
  };

  const openEdit = (s: Schedule) => {
    setEditing(s);
    setForm({
      channelId: s.channelId,
      daysOfWeek: [...s.daysOfWeek],
      startTime: s.startTime,
      endTime: s.endTime,
      isActive: s.isActive,
    });
    setApiError(null);
    setShowForm(true);
  };

  const closeForm = () => { setShowForm(false); setEditing(null); setApiError(null); };

  const toggleDay = (day: number) =>
    setForm(f => ({
      ...f,
      daysOfWeek: f.daysOfWeek.includes(day)
        ? f.daysOfWeek.filter(d => d !== day)
        : [...f.daysOfWeek, day].sort((a, b) => a - b),
    }));

  const submit = () => {
    if (form.daysOfWeek.length === 0) return;
    setApiError(null);
    if (editing) {
      updateMutation.mutate({ id: editing.id, data: { ...form } });
    } else {
      createMutation.mutate({ ...form });
    }
  };

  const isPending = createMutation.isPending || updateMutation.isPending;

  const daySchedules = schedules.filter(s => s.daysOfWeek.includes(selectedDay));

  return (
    <div className="bg-sp-gray rounded-xl px-6 py-5">
      <div className="flex items-center justify-between mb-4">
        <h2 className="text-sp-white font-semibold text-sm">Schedules</h2>
        {!showForm && (
          <button
            onClick={openAdd}
            className="text-xs text-sp-green hover:text-sp-green-hover font-medium transition-colors"
          >
            + Add Schedule
          </button>
        )}
      </div>

      {/* Day selector */}
      <div className="flex gap-1.5 mb-4">
        {DAY_LABELS.map((label, i) => (
          <button
            key={i}
            onClick={() => setSelectedDay(i)}
            className={`flex-1 py-1 rounded text-xs font-medium transition-colors ${
              selectedDay === i
                ? 'bg-sp-green text-sp-black'
                : 'bg-sp-darkgray text-sp-subtext hover:text-sp-white'
            }`}
          >
            {label}
          </button>
        ))}
      </div>

      {/* 24-hour visual timeline */}
      <div className="mb-5">
        <div className="relative h-6 bg-sp-darkgray rounded overflow-hidden">
          {daySchedules.map(s =>
            getSegments(s.startTime, s.endTime).map((seg, idx) => (
              <button
                key={`${s.id}-${idx}`}
                onClick={() => openEdit(s)}
                style={{
                  position: 'absolute',
                  left: `${seg.left}%`,
                  width: `${seg.width}%`,
                  top: 0,
                  bottom: 0,
                  backgroundColor: s.channelAccentColor,
                  opacity: s.isActive ? 1 : 0.4,
                }}
                title={`${s.name} · ${s.startTime}–${s.endTime}`}
              />
            ))
          )}
        </div>
        {/* Hour markers */}
        <div className="relative h-4 mt-1">
          {HOUR_MARKERS.map(hour => (
            <span
              key={hour}
              className="absolute text-xs text-sp-subtext"
              style={{ left: `${(hour / 24) * 100}%`, transform: 'translateX(-50%)' }}
            >
              {formatHourLabel(hour)}
            </span>
          ))}
        </div>
      </div>

      {/* Inline form */}
      {showForm && (
        <div className="bg-sp-darkgray rounded-lg p-4 mb-4 space-y-3">
          <h3 className="text-sp-white text-xs font-semibold">
            {editing ? 'Edit Schedule' : 'New Schedule'}
          </h3>

          {/* Channel */}
          <select
            value={form.channelId}
            onChange={e => setForm(f => ({ ...f, channelId: Number(e.target.value) }))}
            className="w-full bg-sp-lightgray/30 border border-sp-lightgray/40 rounded px-3 py-1.5 text-sp-white text-xs focus:outline-none focus:border-sp-green/50"
          >
            {channels.map(ch => (
              <option key={ch.id} value={ch.id}>{ch.name}</option>
            ))}
          </select>

          {/* Days of week */}
          <div>
            <p className="text-sp-subtext text-xs mb-1.5">Days</p>
            <div className="flex gap-1.5 flex-wrap">
              {DAY_LABELS.map((label, i) => (
                <button
                  key={i}
                  type="button"
                  onClick={() => toggleDay(i)}
                  className={`px-2.5 py-1 rounded text-xs font-medium transition-colors ${
                    form.daysOfWeek.includes(i)
                      ? 'bg-sp-green text-sp-black'
                      : 'bg-sp-lightgray/30 text-sp-subtext hover:text-sp-white'
                  }`}
                >
                  {label}
                </button>
              ))}
            </div>
          </div>

          {/* Times */}
          <div className="flex gap-3">
            <div className="flex-1">
              <p className="text-sp-subtext text-xs mb-1">Start</p>
              <input
                type="time"
                value={form.startTime}
                onChange={e => setForm(f => ({ ...f, startTime: e.target.value }))}
                className="w-full bg-sp-lightgray/30 border border-sp-lightgray/40 rounded px-3 py-1.5 text-sp-white text-xs focus:outline-none focus:border-sp-green/50"
              />
            </div>
            <div className="flex-1">
              <p className="text-sp-subtext text-xs mb-1">End</p>
              <input
                type="time"
                value={form.endTime}
                onChange={e => setForm(f => ({ ...f, endTime: e.target.value }))}
                className="w-full bg-sp-lightgray/30 border border-sp-lightgray/40 rounded px-3 py-1.5 text-sp-white text-xs focus:outline-none focus:border-sp-green/50"
              />
            </div>
          </div>

          {/* Active toggle (edit only) */}
          {editing && (
            <label className="flex items-center gap-2 cursor-pointer">
              <input
                type="checkbox"
                checked={form.isActive}
                onChange={e => setForm(f => ({ ...f, isActive: e.target.checked }))}
                className="accent-sp-green"
              />
              <span className="text-sp-subtext text-xs">Active</span>
            </label>
          )}

          {/* Inline API error */}
          {apiError && (
            <p className="text-red-400 text-xs">{apiError}</p>
          )}

          <div className="flex gap-2 pt-1">
            <button
              onClick={submit}
              disabled={isPending || form.daysOfWeek.length === 0}
              className="bg-sp-green hover:bg-sp-green-hover text-sp-black font-bold px-4 py-1.5 rounded-full text-xs transition-colors disabled:opacity-60"
            >
              {isPending ? 'Saving…' : editing ? 'Save Changes' : 'Create'}
            </button>
            <button
              onClick={closeForm}
              className="text-sp-subtext hover:text-sp-white text-xs px-4 py-1.5 transition-colors"
            >
              Cancel
            </button>
          </div>
        </div>
      )}

      {/* Slot list for selected day */}
      {isLoading ? (
        <p className="text-sp-subtext text-xs text-center py-4">Loading…</p>
      ) : daySchedules.length === 0 ? (
        <p className="text-sp-subtext text-xs text-center py-4">
          No schedules for {DAY_LABELS[selectedDay]}. Add one to automate playback.
        </p>
      ) : (
        <div className="space-y-2">
          {daySchedules.map(s => (
            <div
              key={s.id}
              className={`flex items-start justify-between gap-3 px-3 py-2.5 rounded-lg ${
                s.isActive ? 'bg-sp-darkgray' : 'bg-sp-darkgray/50 opacity-60'
              }`}
            >
              <div className="flex-1 min-w-0">
                <div className="flex items-center gap-2 mb-0.5">
                  <span
                    className="w-2 h-2 rounded-full flex-shrink-0"
                    style={{ backgroundColor: s.channelAccentColor }}
                  />
                  <span className="text-sp-white text-xs font-medium truncate">{s.channelName}</span>
                  {s.isActive ? (
                    <span className="text-xs bg-sp-green/20 text-sp-green px-1.5 py-0.5 rounded">Active</span>
                  ) : (
                    <span className="text-sp-subtext text-xs">(paused)</span>
                  )}
                </div>
                <p className="text-sp-subtext text-xs">
                  {s.daysOfWeek.map(d => DAY_LABELS[d]).join(' ')}
                  {' · '}
                  {s.startTime}–{s.endTime}
                </p>
              </div>
              <div className="flex items-center gap-3 flex-shrink-0">
                <button
                  onClick={() => openEdit(s)}
                  className="text-sp-subtext hover:text-sp-white text-xs transition-colors"
                >
                  Edit
                </button>
                <button
                  onClick={() => {
                    if (window.confirm(`Delete "${s.name}"?`))
                      deleteMutation.mutate(s.id);
                  }}
                  className="text-sp-subtext hover:text-red-400 text-xs transition-colors"
                >
                  Delete
                </button>
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
