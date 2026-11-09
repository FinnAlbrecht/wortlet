import { api } from "../../shared/api/client";

export interface User {
  id: string;
  email: string;
}

export interface AuthResponse {
  token: string;
  expiresAt: string;
  user: User;
}

export const authApi = {
  register: (email: string, password: string) =>
    api<AuthResponse>("/api/auth/register", { method: "POST", body: { email, password } }),
  login: (email: string, password: string) =>
    api<AuthResponse>("/api/auth/login", { method: "POST", body: { email, password } }),
  me: () => api<User>("/api/auth/me"),
};
