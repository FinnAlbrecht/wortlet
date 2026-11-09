import { api } from "../../shared/api/client";

export interface BackupDocument {
  version: number;
  app: string;
  exportedAt: string;
  languages: Array<{ name: string; folders: Array<{ name: string; chapters: Array<{ words: unknown[] }>; weakWords?: string[] }> }>;
}

export interface ImportResult {
  languages: number;
  folders: number;
  words: number;
  weakWords: number;
}

export const backupApi = {
  export: () => api<BackupDocument>("/api/backup/export"),
  import: (doc: unknown) => api<ImportResult>("/api/backup/import", { method: "POST", body: doc }),
};
