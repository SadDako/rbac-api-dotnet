const envBaseUrl = import.meta.env.VITE_API_BASE_URL as string | undefined;

const fallbackBaseUrl =
  typeof window !== "undefined" && window.location.protocol === "https:"
    ? "https://localhost:5083"
    : "http://localhost:5083";

export const API_BASE_URL = envBaseUrl || fallbackBaseUrl;
