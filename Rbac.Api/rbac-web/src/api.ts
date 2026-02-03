import { logActivity } from "./activity";
import { API_BASE_URL } from "./config";
import { emitApiNotice } from "./api-notice";

export type ApiErrorCode = "NETWORK" | "UNAUTHORIZED" | "FORBIDDEN" | "UNKNOWN" | string;

type ProblemDetails = {
  status?: number;
  code?: string;
  message?: string;
  detail?: string;
  title?: string;
  traceId?: string;
  correlationId?: string;
};

export class ApiError extends Error {
  status?: number;
  code?: ApiErrorCode;
  details?: unknown;
  traceId?: string;
  correlationId?: string;

  constructor(
    message: string,
    options?: {
      status?: number;
      code?: ApiErrorCode;
      details?: unknown;
      traceId?: string;
      correlationId?: string;
    }
  ) {
    super(message);
    this.name = "ApiError";
    this.status = options?.status;
    this.code = options?.code;
    this.details = options?.details;
    this.traceId = options?.traceId;
    this.correlationId = options?.correlationId;
  }
}

function createCorrelationId() {
  if (typeof crypto !== "undefined" && "randomUUID" in crypto) {
    return crypto.randomUUID();
  }

  return `${Date.now()}-${Math.random().toString(16).slice(2)}`;
}

async function parseProblemDetails(response: Response): Promise<ProblemDetails | null> {
  const contentType = response.headers.get("content-type") || "";

  if (!contentType.includes("application/json")) {
    const rawText = await response.text();
    if (!rawText) return null;
    return { message: rawText };
  }

  try {
    return (await response.json()) as ProblemDetails;
  } catch {
    return null;
  }
}

async function fetchWithRetry(input: RequestInfo | URL, init: RequestInit, maxAttempts = 2) {
  let attempt = 0;
  let lastError: unknown = null;

  while (attempt < maxAttempts) {
    try {
      return await fetch(input, init);
    } catch (error) {
      lastError = error;
      attempt += 1;
      if (attempt >= maxAttempts) {
        throw lastError;
      }
    }
  }

  throw lastError;
}

function shouldTrackRequest(path: string) {
  return !path.startsWith("/activity");
}

export async function apiFetch(path: string, options: RequestInit = {}) {
  const token = localStorage.getItem("token");
  const url = `${API_BASE_URL}${path}`;
  const method = options.method || "GET";
  const requestCorrelationId = createCorrelationId();

  const headers = new Headers(options.headers || {});
  const bodyIsFormData = options.body instanceof FormData;

  if (!headers.has("Content-Type") && !bodyIsFormData) {
    headers.set("Content-Type", "application/json");
  }

  if (token) {
    headers.set("Authorization", `Bearer ${token}`);
  }

  headers.set("X-Correlation-Id", requestCorrelationId);

  let res: Response;

  try {
    res = await fetchWithRetry(url, { ...options, headers }, 2);
  } catch {
    if (shouldTrackRequest(path)) {
      logActivity({
        type: "api",
        status: "error",
        label: `Network failure: ${path}`,
        description: "Could not connect to the server.",
        correlationId: requestCorrelationId,
      });
    }

    const message = "Could not connect to the server. Please check if the API is online.";

    emitApiNotice({
      variant: "warning",
      title: "Network error",
      message,
    });

    throw new ApiError(message, {
      code: "NETWORK",
      correlationId: requestCorrelationId,
    });
  }

  const responseCorrelationId = res.headers.get("X-Correlation-Id") || requestCorrelationId;

  if (!res.ok) {
    const problem = await parseProblemDetails(res);
    const codeFromProblem = problem?.code;
    const messageFromProblem =
      problem?.message || problem?.detail || problem?.title || `HTTP ${res.status}`;
    const traceId = problem?.traceId;
    const correlationId = problem?.correlationId || responseCorrelationId;

    if (shouldTrackRequest(path)) {
      logActivity({
        type: "api",
        status: "error",
        label: `HTTP ${res.status}: ${path}`,
        description: messageFromProblem,
        correlationId,
      });
    }

    if (res.status === 401) {
      localStorage.removeItem("token");
      localStorage.removeItem("me");

      emitApiNotice({
        variant: "warning",
        title: "Session expired",
        message: messageFromProblem,
      });

      if (window.location.pathname !== "/login") {
        window.location.assign("/login");
      }

      throw new ApiError(messageFromProblem, {
        status: res.status,
        code: codeFromProblem || "UNAUTHORIZED",
        details: problem,
        traceId,
        correlationId,
      });
    }

    if (res.status === 403) {
      emitApiNotice({
        variant: "error",
        title: "Access denied",
        message: messageFromProblem,
      });

      if (window.location.pathname !== "/access-denied") {
        window.location.assign("/access-denied");
      }

      throw new ApiError(messageFromProblem, {
        status: res.status,
        code: codeFromProblem || "FORBIDDEN",
        details: problem,
        traceId,
        correlationId,
      });
    }

    emitApiNotice({
      variant: "error",
      title: "Request failed",
      message: messageFromProblem,
    });

    throw new ApiError(messageFromProblem, {
      status: res.status,
      code: codeFromProblem || "UNKNOWN",
      details: problem,
      traceId,
      correlationId,
    });
  }

  if (shouldTrackRequest(path)) {
    logActivity({
      type: "api",
      status: "success",
      label: `API ${method} ${path}`,
      description: "Request completed successfully.",
      correlationId: responseCorrelationId,
    });
  }

  return res;
}
