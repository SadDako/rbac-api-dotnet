import { API_BASE_URL } from "./config";

export type ActivityType = "auth" | "nav" | "api" | "audit" | "system";
export type ActivityStatus = "success" | "error" | "info";
export type ActivitySource = "local" | "backend" | "client";

export type ActivityEvent = {
  id: string;
  type: ActivityType;
  status: ActivityStatus;
  label: string;
  description?: string;
  at: string;
  source?: ActivitySource;
  actor?: string;
  correlationId?: string;
};

const LOCAL_STORAGE_KEY = "rbac.activity.local.v1";
const MAX_LOCAL_EVENTS = 200;

let localFeed: ActivityEvent[] = loadLocalActivity();

function createId() {
  if (typeof crypto !== "undefined" && "randomUUID" in crypto) {
    return crypto.randomUUID();
  }

  return `${Date.now()}-${Math.random().toString(16).slice(2)}`;
}

function loadLocalActivity() {
  try {
    const raw = localStorage.getItem(LOCAL_STORAGE_KEY);
    if (!raw) return [];
    const parsed = JSON.parse(raw) as ActivityEvent[];
    if (!Array.isArray(parsed)) return [];
    return parsed.slice(0, MAX_LOCAL_EVENTS);
  } catch {
    return [];
  }
}

function persistLocalActivity() {
  localStorage.setItem(LOCAL_STORAGE_KEY, JSON.stringify(localFeed.slice(0, MAX_LOCAL_EVENTS)));
}

function createCorrelationId() {
  if (typeof crypto !== "undefined" && "randomUUID" in crypto) {
    return crypto.randomUUID();
  }
  return `${Date.now()}-${Math.random().toString(16).slice(2)}`;
}

async function sendActivityToBackend(event: ActivityEvent) {
  const token = localStorage.getItem("token");
  if (!token) return;

  try {
    await fetch(`${API_BASE_URL}/activity`, {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
        Authorization: `Bearer ${token}`,
        "X-Correlation-Id": event.correlationId || createCorrelationId(),
      },
      body: JSON.stringify({
        type: event.type,
        status: event.status,
        label: event.label,
        description: event.description,
      }),
    });
  } catch {
    // Activity sync is best-effort and should not block UX.
  }
}

export async function fetchRemoteActivity(limit = 50) {
  const token = localStorage.getItem("token");
  if (!token) return [] as ActivityEvent[];

  try {
    const response = await fetch(`${API_BASE_URL}/activity?limit=${Math.max(1, Math.min(limit, 200))}`, {
      headers: {
        Authorization: `Bearer ${token}`,
        "X-Correlation-Id": createCorrelationId(),
      },
    });

    if (!response.ok) {
      return [] as ActivityEvent[];
    }

    const data = (await response.json()) as Array<{
      id: string;
      type: string;
      status: string;
      label: string;
      description?: string;
      atUtc: string;
      source: string;
      actor?: string;
      correlationId?: string;
    }>;

    return data.map((event) => ({
      id: event.id,
      type: (event.type || "system") as ActivityType,
      status: (event.status || "info") as ActivityStatus,
      label: event.label,
      description: event.description,
      at: event.atUtc,
      source: (event.source === "client" ? "client" : "backend") as ActivitySource,
      actor: event.actor,
      correlationId: event.correlationId,
    }));
  } catch {
    return [] as ActivityEvent[];
  }
}

export function mergeActivityEvents(localEvents: ActivityEvent[], remoteEvents: ActivityEvent[], limit = 250) {
  const map = new Map<string, ActivityEvent>();

  [...localEvents, ...remoteEvents].forEach((event) => {
    const key = `${event.id}:${event.at}:${event.label}`;
    if (!map.has(key)) {
      map.set(key, event);
    }
  });

  return [...map.values()]
    .sort((a, b) => new Date(b.at).getTime() - new Date(a.at).getTime())
    .slice(0, limit);
}

export function logActivity(
  event: Omit<ActivityEvent, "id" | "at" | "source"> & { id?: string; at?: string; source?: ActivitySource },
  options?: { sync?: boolean }
) {
  const normalized: ActivityEvent = {
    id: event.id ?? createId(),
    at: event.at ?? new Date().toISOString(),
    type: event.type,
    status: event.status,
    label: event.label,
    description: event.description,
    source: event.source ?? "local",
    actor: event.actor,
    correlationId: event.correlationId,
  };

  localFeed.unshift(normalized);

  if (localFeed.length > MAX_LOCAL_EVENTS) {
    localFeed.length = MAX_LOCAL_EVENTS;
  }

  persistLocalActivity();
  window.dispatchEvent(new CustomEvent("rbac-activity"));

  if (options?.sync === false) return;
  void sendActivityToBackend(normalized);
}

export function getActivityFeed() {
  return [...localFeed];
}

export function clearActivityFeed() {
  localFeed = [];
  persistLocalActivity();
  window.dispatchEvent(new CustomEvent("rbac-activity"));
}
