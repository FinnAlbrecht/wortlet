import { api } from "../../shared/api/client";

export interface FolderSummary {
  id: string;
  languageId: string;
  name: string;
  description: string;
  isSeed: boolean;
  chapterCount: number;
  wordCount: number;
  weakCount: number;
  createdAt: string;
}

export interface Word {
  /** Deutscher Eintrag, eindeutig im Ordner (Duplikate mit „ (2)“). */
  key: string;
  /** Deutscher Eintrag ohne Duplikat-Nummer – so wird er abgefragt. */
  german: string;
  answers: string[];
  /** Lösung zur Anzeige (ohne reine Tastatur-Varianten). */
  solution: string;
  hint?: string | null;
}

export interface Chapter {
  name: string;
  pages: string;
  isNew: boolean;
  words: Word[];
}

export interface FolderDetail {
  id: string;
  languageId: string;
  languageName: string;
  name: string;
  description: string;
  isSeed: boolean;
  oldLabel: string;
  newLabel: string;
  text: string;
  chapters: Chapter[];
  weakWords: string[];
}

export const foldersApi = {
  get: (id: string) => api<FolderDetail>(`/api/folders/${id}`),
  create: (languageId: string, name: string, text: string) =>
    api<FolderDetail>("/api/folders", { method: "POST", body: { languageId, name, text } }),
  update: (id: string, name: string, text: string) =>
    api<FolderDetail>(`/api/folders/${id}`, { method: "PUT", body: { name, text } }),
  remove: (id: string) => api<void>(`/api/folders/${id}`, { method: "DELETE" }),
};
