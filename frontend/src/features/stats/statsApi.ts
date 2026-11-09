import { api } from "../../shared/api/client";
import type { Direction } from "../quiz/direction";

export interface DayStat {
  day: string;
  answers: number;
  correct: number;
}

export interface StatsOverview {
  totalAnswers: number;
  correctAnswers: number;
  wordsPracticed: number;
  quizzes: number;
  averageGrade: number | null;
  currentStreak: number;
  longestStreak: number;
  todayAnswers: number;
  weekAnswers: number;
  lastDays: DayStat[];
}

export interface QuizResult {
  id: string;
  folderId: string | null;
  setId: string | null;
  title: string;
  direction: Direction;
  practiceMode: boolean;
  wordCount: number;
  firstRoundCorrect: number;
  grade: number;
  finishedAt: string;
}

export interface HardWord {
  folderId: string;
  folderName: string;
  german: string;
  solution: string;
  attempts: number;
  wrong: number;
}

export interface SaveResult {
  folderId?: string;
  setId?: string;
  direction: Direction;
  practiceMode: boolean;
  wordCount: number;
  firstRoundCorrect: number;
}

export const statsApi = {
  overview: () => api<StatsOverview>("/api/stats/overview"),
  results: (take = 50) => api<QuizResult[]>(`/api/stats/results?take=${take}`),
  hardest: (languageId?: string, take = 10) =>
    api<HardWord[]>(`/api/stats/hardest?take=${take}${languageId ? `&languageId=${languageId}` : ""}`),
  saveResult: (body: SaveResult) => api<QuizResult>("/api/stats/results", { method: "POST", body }),
};
