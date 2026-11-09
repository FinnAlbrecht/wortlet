import { createContext, useCallback, useContext, useMemo, useState, type ReactNode } from "react";
import { configureApi } from "../../shared/api/client";
import { authApi, type AuthResponse, type User } from "./authApi";

const STORAGE_KEY = "wortlet-auth";

interface Session {
  token: string;
  expiresAt: string;
  user: User;
}

interface AuthContextValue {
  user: User | null;
  login: (email: string, password: string) => Promise<void>;
  register: (email: string, password: string) => Promise<void>;
  logout: () => void;
}

const AuthContext = createContext<AuthContextValue | null>(null);

function readSession(): Session | null {
  try {
    const raw = localStorage.getItem(STORAGE_KEY);
    if (!raw) return null;
    const s = JSON.parse(raw) as Session;
    if (!s.token || new Date(s.expiresAt).getTime() < Date.now()) return null;
    return s;
  } catch {
    return null;
  }
}

function writeSession(s: Session | null) {
  try {
    if (s) localStorage.setItem(STORAGE_KEY, JSON.stringify(s));
    else localStorage.removeItem(STORAGE_KEY);
  } catch {
    // privater Modus: Sitzung gilt nur bis zum Neuladen
  }
}

let currentToken: string | null = readSession()?.token ?? null;

export function AuthProvider({ children }: { children: ReactNode }) {
  const [session, setSession] = useState<Session | null>(() => readSession());

  const apply = useCallback((s: Session | null) => {
    currentToken = s?.token ?? null;
    writeSession(s);
    setSession(s);
  }, []);

  const logout = useCallback(() => apply(null), [apply]);

  // Einmalig: API-Client mit Token und Abmelde-Handler verbinden.
  useState(() => configureApi({ getToken: () => currentToken, onUnauthorized: () => apply(null) }));

  const fromResponse = useCallback((r: AuthResponse) => apply({ token: r.token, expiresAt: r.expiresAt, user: r.user }), [apply]);

  const value = useMemo<AuthContextValue>(
    () => ({
      user: session?.user ?? null,
      login: async (email, password) => fromResponse(await authApi.login(email, password)),
      register: async (email, password) => fromResponse(await authApi.register(email, password)),
      logout,
    }),
    [session, fromResponse, logout],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthContextValue {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error("useAuth außerhalb von AuthProvider");
  return ctx;
}
