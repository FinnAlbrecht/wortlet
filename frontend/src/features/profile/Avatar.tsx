import { useEffect, useState } from "react";
import { apiBlob } from "../../shared/api/client";
import type { PublicProfile } from "./profileApi";

// Ein Bild pro Person und Version nur einmal laden.
const cache = new Map<string, Promise<string | null>>();

function loadImage(userId: string, version: number): Promise<string | null> {
  const key = `${userId}:${version}`;
  let pending = cache.get(key);
  if (!pending) {
    pending = apiBlob(`/api/profile/${userId}/avatar`)
      .then((blob) => URL.createObjectURL(blob))
      .catch(() => {
        cache.delete(key); // später nochmals versuchen
        return null;
      });
    cache.set(key, pending);
  }
  return pending;
}

type AvatarData = Pick<PublicProfile, "userId" | "displayName" | "avatarIcon" | "avatarColor" | "hasImage" | "avatarVersion">;

/** Rundes Profilbild: eigenes Bild oder Symbol auf Farbe. */
export function Avatar({ profile, size = 40 }: { profile: AvatarData; size?: number }) {
  const [src, setSrc] = useState<string | null>(null);

  useEffect(() => {
    let active = true;
    setSrc(null);
    if (profile.hasImage)
      void loadImage(profile.userId, profile.avatarVersion).then((url) => {
        if (active) setSrc(url);
      });
    return () => {
      active = false;
    };
  }, [profile.userId, profile.hasImage, profile.avatarVersion]);

  const style = { width: size, height: size, fontSize: Math.round(size * 0.55), background: profile.avatarColor };
  return (
    <span className="avatar" style={style} role="img" aria-label={`Profilbild von ${profile.displayName}`}>
      {src ? <img src={src} alt="" /> : <span aria-hidden="true">{profile.avatarIcon}</span>}
    </span>
  );
}
