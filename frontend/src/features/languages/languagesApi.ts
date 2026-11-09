import { api } from "../../shared/api/client";
import type { FolderSummary } from "../folders/foldersApi";

export interface Language {
  id: string;
  name: string;
  folderCount: number;
  wordCount: number;
  weakCount: number;
  createdAt: string;
}

export const languagesApi = {
  list: () => api<Language[]>("/api/languages"),
  get: (id: string) => api<Language>(`/api/languages/${id}`),
  create: (name: string) => api<Language>("/api/languages", { method: "POST", body: { name } }),
  rename: (id: string, name: string) => api<Language>(`/api/languages/${id}`, { method: "PUT", body: { name } }),
  remove: (id: string) => api<void>(`/api/languages/${id}`, { method: "DELETE" }),
  folders: (id: string) => api<FolderSummary[]>(`/api/languages/${id}/folders`),
};

export const languageCode = (name: string) => name.slice(0, 2).toUpperCase();
