import { API_BASE_URL } from "./config";

export function getToken() {
  return localStorage.getItem("token");
}

export async function apiFetch(path: string, options: RequestInit = {}) {
  const token = getToken();

  const headers = new Headers(options.headers || {});
  headers.set("Content-Type", "application/json");

  if (token) headers.set("Authorization", `Bearer ${token}`);

  const res = await fetch(`${API_BASE_URL}${path}`, { ...options, headers });

  if (!res.ok) {
    let msg = `HTTP ${res.status}`;
    try {
      const body = await res.json();
      msg = body?.message || msg;
    } catch {}
    throw new Error(msg);
  }

  return res;
}
