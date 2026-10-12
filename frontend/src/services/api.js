import axios from "axios";
import { authStorage } from "../utils/authStorage";
import { normalizeError } from "../utils/http";

const baseURL = import.meta.env.VITE_API_BASE_URL;

const api = axios.create({
  baseURL,
  timeout: 30000,
  headers: {
    "Content-Type": "application/json",
  },
});

let onUnauthorized = null;

export function setUnauthorizedHandler(handler) {
  onUnauthorized = handler;
}

api.interceptors.request.use((config) => {
  if (!baseURL) {
    config.metadata = { ...(config.metadata || {}), missingBaseUrl: true };
  }

  const token = authStorage.getToken();
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }

  if (config.data instanceof FormData) {
    delete config.headers["Content-Type"];
  }

  return config;
});

api.interceptors.response.use(
  (response) => response,
  (error) => {
    const normalized = normalizeError(error);
    error.normalized = normalized;

    const requestUrl = String(error.config?.url || "");
    const isAuthAttempt =
      requestUrl.includes("/api/auth/login") ||
      requestUrl.includes("/api/auth/register");

    if (error.response?.status === 401 && !isAuthAttempt && onUnauthorized) {
      onUnauthorized();
    }

    return Promise.reject(error);
  },
);

export function getApiBaseUrl() {
  return baseURL;
}

export default api;
