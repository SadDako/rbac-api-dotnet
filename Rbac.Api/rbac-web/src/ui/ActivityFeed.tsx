import { useEffect, useMemo, useState } from "react";
import { clearActivityFeed, fetchRemoteActivity, mergeActivityEvents } from "../activity";
import type { ActivityEvent, ActivityStatus, ActivityType } from "../activity";
import Button from "./Button";
import Skeleton from "./Skeleton";

const statusLabels: Record<ActivityStatus, string> = {
  success: "Success",
  error: "Error",
  info: "Info",
};

const typeLabels: Record<ActivityType, string> = {
  auth: "Auth",
  api: "API",
  nav: "Navigation",
  audit: "Audit",
  system: "System",
};

type ActivityFeedProps = {
  events: ActivityEvent[];
};

export default function ActivityFeed({ events }: ActivityFeedProps) {
  const [remoteEvents, setRemoteEvents] = useState<ActivityEvent[]>([]);
  const [loadingRemote, setLoadingRemote] = useState(true);
  const [nowMs, setNowMs] = useState(() => Date.now());
  const [typeFilter, setTypeFilter] = useState<ActivityType | "all">("all");
  const [statusFilter, setStatusFilter] = useState<ActivityStatus | "all">("all");
  const [periodFilter, setPeriodFilter] = useState<"24h" | "7d" | "all">("24h");

  async function loadRemote() {
    setLoadingRemote(true);
    const data = await fetchRemoteActivity(100);
    setRemoteEvents(data);
    setLoadingRemote(false);
  }

  useEffect(() => {
    loadRemote();
  }, []);

  useEffect(() => {
    const timer = window.setInterval(() => setNowMs(Date.now()), 60_000);
    return () => window.clearInterval(timer);
  }, []);

  const mergedEvents = useMemo(() => mergeActivityEvents(events, remoteEvents, 250), [events, remoteEvents]);

  const availableTypes = useMemo(() => {
    const discovered = new Set<ActivityType>();
    mergedEvents.forEach((event) => discovered.add(event.type));

    if (discovered.size === 0) {
      return ["auth", "api", "nav"] as ActivityType[];
    }

    return [...discovered];
  }, [mergedEvents]);

  const filtered = useMemo(() => {
    const periodMs =
      periodFilter === "24h" ? 24 * 60 * 60 * 1000 : periodFilter === "7d" ? 7 * 24 * 60 * 60 * 1000 : Infinity;

    return mergedEvents.filter((event) => {
      const typeOk = typeFilter === "all" || event.type === typeFilter;
      const statusOk = statusFilter === "all" || event.status === statusFilter;
      const ageMs = nowMs - new Date(event.at).getTime();
      const periodOk = periodMs === Infinity || ageMs <= periodMs;
      return typeOk && statusOk && periodOk;
    });
  }, [mergedEvents, nowMs, periodFilter, statusFilter, typeFilter]);

  function badgeClassForStatus(status: ActivityStatus) {
    if (status === "success") return "badge--success";
    if (status === "error") return "badge--danger";
    return "badge--info";
  }

  return (
    <div className="activity">
      <div className="activity-header">
        <div>
          <h3>Activity feed</h3>
          <p className="muted">Unified local and backend events with filters and timeline.</p>
        </div>
        <div className="activity-actions">
          <Button variant="ghost" onClick={loadRemote} disabled={loadingRemote}>
            Refresh
          </Button>
          <Button variant="ghost" onClick={() => clearActivityFeed()}>
            Clear local history
          </Button>
        </div>
      </div>

      <div className="activity-filters">
        <label>
          Type
          <select value={typeFilter} onChange={(event) => setTypeFilter(event.target.value as ActivityType | "all")}>
            <option value="all">All</option>
            {availableTypes.map((type) => (
              <option key={type} value={type}>
                {typeLabels[type]}
              </option>
            ))}
          </select>
        </label>

        <label>
          Status
          <select value={statusFilter} onChange={(event) => setStatusFilter(event.target.value as ActivityStatus | "all")}>
            <option value="all">All</option>
            <option value="success">Success</option>
            <option value="error">Error</option>
            <option value="info">Info</option>
          </select>
        </label>

        <label>
          Period
          <select value={periodFilter} onChange={(event) => setPeriodFilter(event.target.value as "24h" | "7d" | "all")}>
            <option value="24h">Last 24h</option>
            <option value="7d">Last 7 days</option>
            <option value="all">All time</option>
          </select>
        </label>
      </div>

      {loadingRemote && (
        <div className="skeleton-stack">
          <Skeleton className="skeleton-line" />
          <Skeleton className="skeleton-line" />
        </div>
      )}

      <div className="activity-timeline">
        {!loadingRemote && filtered.length === 0 && (
          <div className="empty-state">
            <strong>No events found</strong>
            <p>Try another type/status/period filter.</p>
          </div>
        )}

        {filtered.map((event) => (
          <div key={`${event.id}-${event.at}`} className={`activity-item status-${event.status}`}>
            <div className="activity-dot" />
            <div>
              <div className="activity-meta">
                <span className="badge badge--info">{typeLabels[event.type]}</span>
                <span className={`badge ${badgeClassForStatus(event.status)}`}>{statusLabels[event.status]}</span>
                <span className="badge badge--default">{event.source || "local"}</span>
                <time>{new Date(event.at).toLocaleString()}</time>
              </div>

              <strong>{event.label}</strong>
              {event.description && <p>{event.description}</p>}
              {event.actor && <small className="muted">Actor: {event.actor}</small>}
              {event.correlationId && <small className="muted">Correlation: {event.correlationId}</small>}
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}
