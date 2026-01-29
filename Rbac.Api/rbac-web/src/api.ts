import { API_BASE_URL } from "./config";

const isDev = import.meta.env.DEV;

function logDebug(message: string, data?: unknown) {
  if (!isDev) return;
  if (data) {
    console.debug(`[apiFetch] ${message}`, data);
  } else {
    console.debug(`[apiFetch] ${message}`);
  }
}

export async function apiFetch(path: string, options: RequestInit = {}) {
  const token = localStorage.getItem("token");

  const headers = new Headers(options.headers || {});
  if (!headers.has("Content-Type")) headers.set("Content-Type", "application/json");
  if (token) headers.set("Authorization", `Bearer ${token}`);

  const url = `${API_BASE_URL}${path}`;
  logDebug("request", { url, method: options.method || "GET" });

  const res = await fetch(url, { ...options, headers });

  if (!res.ok) {
    let msg = `HTTP ${res.status}`;
    try {
      const body = await res.json();
      msg = body?.message || msg;
    } catch {
      // ignore JSON parse errors
    }

    logDebug("error", { status: res.status, message: msg });

    if (res.status === 401) {
      localStorage.removeItem("token");
      localStorage.removeItem("me");
      if (window.location.pathname !== "/login") {
        window.location.href = "/login";
      }
    }

    throw new Error(msg);
  }

  return res;
}
