import { API_BASE_URL } from "./config";

export type ApiErrorCode = "NETWORK" | "UNAUTHORIZED" | "FORBIDDEN" | "UNKNOWN";

export class ApiError extends Error {
  status?: number;
  code?: ApiErrorCode;
  details?: unknown;

  constructor(message: string, options?: { status?: number; code?: ApiErrorCode; details?: unknown }) {
    super(message);
    this.name = "ApiError";
    this.status = options?.status;
    this.code = options?.code;
    this.details = options?.details;
  }
}

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

  let res: Response;
  try {
    res = await fetch(url, { ...options, headers });
  } catch (error) {
    logDebug("network-error", error);
    throw new ApiError(
      "Não foi possível conectar ao servidor. Verifique se a API está online.",
      { code: "NETWORK" }
    );
  }

  if (!res.ok) {
    let msg = `HTTP ${res.status}`;
    let details: unknown;
    try {
      const body = await res.json();
      details = body;
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
      throw new ApiError("Sessão expirada. Faça login novamente.", {
        status: res.status,
        code: "UNAUTHORIZED",
        details,
      });
    }

    if (res.status === 403) {
      throw new ApiError("Acesso negado para este recurso.", {
        status: res.status,
        code: "FORBIDDEN",
        details,
      });
    }

    throw new ApiError(msg, { status: res.status, code: "UNKNOWN", details });
  }

  return res;
}
