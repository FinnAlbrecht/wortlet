import { api } from "../../shared/api/client";
import type { PublicProfile } from "../profile/profileApi";

/** private: nur per Code · onRequest: in der Suche, Beitritt per Anfrage · public: in der Suche, direkt beitreten */
export type Visibility = "private" | "onRequest" | "public";

export const VISIBILITY: Record<Visibility, { label: string; text: string }> = {
  private: { label: "Privat", text: "Nicht in der Suche. Beitreten nur mit dem Code." },
  onRequest: { label: "Auf Anfrage", text: "In der Suche sichtbar. Wer beitreten will, stellt eine Anfrage – du nimmst an oder lehnst ab." },
  public: { label: "Öffentlich", text: "In der Suche sichtbar. Jeder kann direkt beitreten." },
};

export interface GroupSummary {
  id: string;
  name: string;
  visibility: Visibility;
  isLeader: boolean;
  memberCount: number;
  shareCount: number;
  /** Offene Beitrittsanfragen (nur für die Leitung, sonst 0). */
  pendingRequests: number;
}

export interface GroupSearchResult {
  id: string;
  name: string;
  visibility: Visibility;
  memberCount: number;
  leaderName: string;
  status: "member" | "requested" | "none";
}

export interface MyRequest {
  groupId: string;
  name: string;
  requestedAt: string;
}

export interface JoinRequest {
  profile: PublicProfile;
  requestedAt: string;
}

export interface RankingEntry {
  profile: PublicProfile;
  isLeader: boolean;
  isMe: boolean;
  weekAnswers: number;
  currentStreak: number;
}

export interface GroupShare {
  id: string;
  kind: "folder" | "set";
  name: string;
  languageName: string;
  wordCount: number;
  sharedAt: string;
  /** Von mir geteilt – dann gibt es nichts zu übernehmen. */
  isMine: boolean;
  myCopyFolderId: string | null;
}

export interface GroupDetail {
  id: string;
  name: string;
  joinCode: string;
  visibility: Visibility;
  isLeader: boolean;
  weekStart: string;
  ranking: RankingEntry[];
  shares: GroupShare[];
  /** Nur für die Leitung gefüllt. */
  joinRequests: JoinRequest[];
}

export interface CopyResult {
  folderId: string;
  languageId: string;
  updated: boolean;
}

export const groupsApi = {
  list: () => api<GroupSummary[]>("/api/groups"),
  get: (id: string) => api<GroupDetail>(`/api/groups/${id}`),
  create: (name: string, visibility: Visibility) => api<GroupDetail>("/api/groups", { method: "POST", body: { name, visibility } }),
  search: (q: string, signal?: AbortSignal) => api<GroupSearchResult[]>(`/api/groups/search?q=${encodeURIComponent(q)}`, { signal }),
  joinFromSearch: (id: string) => api<{ groupId: string; status: "joined" | "requested" }>(`/api/groups/${id}/join`, { method: "POST" }),
  myRequests: () => api<MyRequest[]>("/api/groups/requests"),
  cancelRequest: (id: string) => api<void>(`/api/groups/${id}/request`, { method: "DELETE" }),
  acceptRequest: (id: string, userId: string) => api<GroupDetail>(`/api/groups/${id}/requests/${userId}/accept`, { method: "POST" }),
  declineRequest: (id: string, userId: string) => api<GroupDetail>(`/api/groups/${id}/requests/${userId}`, { method: "DELETE" }),
  setVisibility: (id: string, visibility: Visibility) => api<GroupDetail>(`/api/groups/${id}/visibility`, { method: "PUT", body: { visibility } }),
  join: (code: string) => api<GroupDetail>("/api/groups/join", { method: "POST", body: { code } }),
  rename: (id: string, name: string) => api<GroupDetail>(`/api/groups/${id}`, { method: "PUT", body: { name } }),
  newCode: (id: string) => api<GroupDetail>(`/api/groups/${id}/code`, { method: "POST" }),
  leave: (id: string) => api<void>(`/api/groups/${id}/leave`, { method: "POST" }),
  remove: (id: string) => api<void>(`/api/groups/${id}`, { method: "DELETE" }),
  removeMember: (id: string, userId: string) => api<GroupDetail>(`/api/groups/${id}/members/${userId}`, { method: "DELETE" }),
  share: (id: string, target: { folderId?: string; setId?: string }) =>
    api<GroupDetail>(`/api/groups/${id}/shares`, { method: "POST", body: target }),
  unshare: (id: string, shareId: string) => api<GroupDetail>(`/api/groups/${id}/shares/${shareId}`, { method: "DELETE" }),
  copy: (id: string, shareId: string) => api<CopyResult>(`/api/groups/${id}/shares/${shareId}/copy`, { method: "POST", body: {} }),
};
