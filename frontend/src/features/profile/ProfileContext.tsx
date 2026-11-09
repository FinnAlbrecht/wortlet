import { createContext, useCallback, useContext, useEffect, useState, type ReactNode } from "react";
import { useAuth } from "../auth/AuthContext";
import { profileApi, type Profile } from "./profileApi";

interface ProfileContextValue {
  profile: Profile | null;
  setProfile: (p: Profile) => void;
}

const ProfileContext = createContext<ProfileContextValue | null>(null);

/** Das eigene Profil – für die Kopfzeile und die Profilseite. */
export function ProfileProvider({ children }: { children: ReactNode }) {
  const { user } = useAuth();
  const [profile, setProfileState] = useState<Profile | null>(null);

  useEffect(() => {
    setProfileState(null);
    if (!user) return;
    let active = true;
    profileApi
      .get()
      .then((p) => active && setProfileState(p))
      .catch(() => {
        // Kopfzeile zeigt dann die E-Mail-Adresse
      });
    return () => {
      active = false;
    };
  }, [user]);

  const setProfile = useCallback((p: Profile) => setProfileState(p), []);
  return <ProfileContext.Provider value={{ profile, setProfile }}>{children}</ProfileContext.Provider>;
}

export function useProfile(): ProfileContextValue {
  const ctx = useContext(ProfileContext);
  if (!ctx) throw new Error("useProfile außerhalb von ProfileProvider");
  return ctx;
}
