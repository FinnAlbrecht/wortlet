import { api } from "../../shared/api/client";
import type { Word } from "../folders/foldersApi";

export interface SetSummary {
  id: string;
  languageId: string;
  name: string;
  wordCount: number;
  createdAt: string;
}

/** Wort im Set – mit dem Ordner, aus dem es stammt. */
export interface SetWord extends Word {
  folderId: string;
  folderName: string;
  isWeak: boolean;
}

export interface SetDetail {
  id: string;
  languageId: string;
  languageName: string;
  name: string;
  words: SetWord[];
}

export interface SetItem {
  folderId: string;
  wordKey: string;
}

export const setsApi = {
  list: (languageId: string) => api<SetSummary[]>(`/api/languages/${languageId}/sets`),
  get: (id: string) => api<SetDetail>(`/api/sets/${id}`),
  create: (languageId: string, name: string, items: SetItem[]) =>
    api<SetDetail>("/api/sets", { method: "POST", body: { languageId, name, items } }),
  update: (id: string, name: string, items: SetItem[]) =>
    api<SetDetail>(`/api/sets/${id}`, { method: "PUT", body: { name, items } }),
  remove: (id: string) => api<void>(`/api/sets/${id}`, { method: "DELETE" }),
};
