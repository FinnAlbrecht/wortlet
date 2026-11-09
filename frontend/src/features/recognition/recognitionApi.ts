import { api } from "../../shared/api/client";

export interface RecognitionStatus {
  configured: boolean;
  dailyLimit: number;
  usedToday: number;
  remainingToday: number;
}

export interface RecognitionResult {
  lines: string[];
  wordCount: number;
  model: string;
  remainingToday: number;
}

export const recognitionApi = {
  status: () => api<RecognitionStatus>("/api/recognize/status"),
  recognize: (photo: Blob, languageName: string) => {
    const form = new FormData();
    form.append("photo", photo, "seite.jpg");
    form.append("language", languageName);
    return api<RecognitionResult>("/api/recognize", { method: "POST", form });
  },
};
