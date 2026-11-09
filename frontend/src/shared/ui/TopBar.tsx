import { useState } from "react";
import { Link, NavLink, useLocation } from "react-router";
import { useAuth } from "../../features/auth/AuthContext";
import { Avatar } from "../../features/profile/Avatar";
import { useProfile } from "../../features/profile/ProfileContext";

type Theme = "auto" | "light" | "dark";
const THEME_KEY = "wortlet-theme";
const LABEL: Record<Theme, string> = { auto: "Design: Auto", light: "Design: Hell", dark: "Design: Dunkel" };

function readTheme(): Theme {
  try {
    const t = localStorage.getItem(THEME_KEY);
    return t === "light" || t === "dark" ? t : "auto";
  } catch {
    return "auto";
  }
}

export function TopBar() {
  const { user, logout } = useAuth();
  const { profile } = useProfile();
  const { pathname } = useLocation();
  const practicing = !/^\/(stats|groups|profile)/.test(pathname);
  const [theme, setTheme] = useState<Theme>(readTheme);

  function cycleTheme() {
    const next: Theme = theme === "auto" ? "light" : theme === "light" ? "dark" : "auto";
    setTheme(next);
    if (next === "auto") delete document.documentElement.dataset.theme;
    else document.documentElement.dataset.theme = next;
    try {
      if (next === "auto") localStorage.removeItem(THEME_KEY);
      else localStorage.setItem(THEME_KEY, next);
    } catch {
      // egal
    }
  }

  return (
    <div className="topbar">
      <div className="toprow">
        <Link to="/" className="brand">Wortlet</Link>
        <div className="who">
          <button type="button" className="ghost small" id="theme" onClick={cycleTheme}>{LABEL[theme]}</button>
          {user && (
            <>
              <Link to="/profile" className="me" id="profilelink" title={`Profil (${user.email})`}>
                {profile && <Avatar profile={profile} size={28} />}
                <span className="email">{profile?.displayName ?? user.email}</span>
              </Link>
              <button type="button" className="ghost small" id="logout" onClick={logout}>Abmelden</button>
            </>
          )}
        </div>
      </div>
      {user && (
        <nav className="mainnav" aria-label="Hauptnavigation">
          <Link to="/" className={practicing ? "active" : undefined} aria-current={practicing ? "page" : undefined}>Üben</Link>
          <NavLink to="/stats">Statistik</NavLink>
          <NavLink to="/groups">Gruppen</NavLink>
        </nav>
      )}
    </div>
  );
}
