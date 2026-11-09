import { api } from "../../shared/api/client";

export type ApiDirection = "toForeign" | "toGerman";

export interface AnswerResult {
  correct: boolean;
  acceptedAnswers: string[];
  solution: string;
  wasWeakBefore: boolean;
  weakCount: number;
}

export const quizApi = {
  answer: (folderId: string, body: { wordKey: string; direction: ApiDirection; input: string; skipped: boolean; round: number; practiceMode: boolean }) =>
    api<AnswerResult>(`/api/folders/${folderId}/quiz/answers`, { method: "POST", body }),
  override: (folderId: string, body: { wordKey: string; round: number; practiceMode: boolean; wasWeakBefore: boolean }) =>
    api<{ weakCount: number }>(`/api/folders/${folderId}/quiz/override`, { method: "POST", body }),
};
