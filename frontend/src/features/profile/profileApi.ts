import { api } from "../../shared/api/client";

export interface PublicProfile {
  userId: string;
  displayName: string;
  avatarIcon: string;
  avatarColor: string;
  hasImage: boolean;
  /** Ändert sich mit jedem Speichern – damit ein neues Bild nicht aus dem Zwischenspeicher kommt. */
  avatarVersion: number;
}

export interface Profile extends PublicProfile {
  email: string;
}

export interface AvatarOptions {
  icons: string[];
  colors: string[];
}

export const profileApi = {
  get: () => api<Profile>("/api/profile"),
  options: () => api<AvatarOptions>("/api/profile/options"),
  update: (body: { displayName: string; avatarIcon: string; avatarColor: string }) =>
    api<Profile>("/api/profile", { method: "PUT", body }),
  uploadImage: (image: Blob) => {
    const form = new FormData();
    form.append("image", image, "avatar.jpg");
    return api<Profile>("/api/profile/avatar", { method: "PUT", form });
  },
  removeImage: () => api<Profile>("/api/profile/avatar", { method: "DELETE" }),
};
