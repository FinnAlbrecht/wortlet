// Dünner fetch-Wrapper: hängt das JWT an, liest ProblemDetails und liefert deutsche Fehlermeldungen.

export class ApiError extends Error {
  constructor(
    message: string,
    public readonly status: number,
    public readonly code?: string,
  ) {
    super(message);
    this.name = "ApiError";
  }
}

let tokenProvider: () => string | null = () => null;
let unauthorizedHandler: () => void = () => {};

export function configureApi(options: { getToken: () => string | null; onUnauthorized: () => void }) {
  tokenProvider = options.getToken;
  unauthorizedHandler = options.onUnauthorized;
}

interface RequestOptions {
  method?: "GET" | "POST" | "PUT" | "DELETE";
  body?: unknown;
  form?: FormData;
  signal?: AbortSignal;
}

function fallbackMessage(status: number): string {
  if (status === 401) return "Bitte melde dich an.";
  if (status === 403) return "Dafür hast du keine Berechtigung.";
  if (status === 404) return "Das wurde nicht gefunden.";
  if (status === 413) return "Die Datei ist zu gross.";
  if (status === 429) return "Zu viele Anfragen. Bitte warte einen Moment.";
  if (status >= 500) return "Der Server hat ein Problem. Bitte versuche es gleich nochmals.";
  return "Das hat nicht geklappt.";
}

export async function api<T>(path: string, options: RequestOptions = {}): Promise<T> {
  const headers: Record<string, string> = { Accept: "application/json" };
  const token = tokenProvider();
  if (token) headers.Authorization = `Bearer ${token}`;

  let body: BodyInit | undefined;
  if (options.form) body = options.form;
  else if (options.body !== undefined) {
    headers["Content-Type"] = "application/json";
    body = JSON.stringify(options.body);
  }

  let response: Response;
  try {
    response = await fetch(path, { method: options.method ?? "GET", headers, body, signal: options.signal });
  } catch (e) {
    if (e instanceof DOMException && e.name === "AbortError") throw e;
    throw new ApiError("Keine Verbindung zum Server. Bist du online?", 0, "network");
  }

  if (response.status === 204) return undefined as T;

  const text = await response.text();
  let data: unknown = undefined;
  if (text) {
    try {
      data = JSON.parse(text);
    } catch {
      data = undefined;
    }
  }

  if (!response.ok) {
    const problem = (data ?? {}) as { title?: string; detail?: string; code?: string };
    if (response.status === 401 && token && !path.startsWith("/api/auth/login")) unauthorizedHandler();
    throw new ApiError(problem.detail || problem.title || fallbackMessage(response.status), response.status, problem.code);
  }
  return data as T;
}

export const errorMessage = (e: unknown): string =>
  e instanceof ApiError ? e.message : e instanceof Error ? e.message : "Unbekannter Fehler.";
