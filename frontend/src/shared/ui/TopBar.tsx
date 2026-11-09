import { useState } from "react";
import { Link } from "react-router";
import { useAuth } from "../../features/auth/AuthContext";

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
      <Link to="/" className="brand">Wortlet</Link>
      <div className="who">
        <button type="button" className="ghost small" id="theme" onClick={cycleTheme}>{LABEL[theme]}</button>
        {user && (
          <>
            <span className="email" title={user.email}>{user.email}</span>
            <button type="button" className="ghost small" id="logout" onClick={logout}>Abmelden</button>
          </>
        )}
      </div>
    </div>
  );
}
