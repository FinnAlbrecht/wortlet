import { useState, type FormEvent } from "react";
import { errorMessage } from "../../shared/api/client";
import { PageHeader } from "../../shared/ui/PageHeader";
import { useAuth } from "./AuthContext";

type Mode = "login" | "register";

export function LoginPage() {
  const { login, register } = useAuth();
  const [mode, setMode] = useState<Mode>("login");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState("");
  const [busy, setBusy] = useState(false);

  async function submit(e: FormEvent) {
    e.preventDefault();
    setError("");
    if (mode === "register" && password.length < 8) {
      setError("Das Passwort muss mindestens 8 Zeichen lang sein.");
      return;
    }
    setBusy(true);
    try {
      if (mode === "login") await login(email, password);
      else await register(email, password);
    } catch (err) {
      setError(errorMessage(err));
      setBusy(false);
    }
  }

  return (
    <>
      <PageHeader title="Vokabeltrainer" subtitle="Melde dich an, damit dein Stand auf Handy und Computer derselbe ist." />
      <section className="card">
        <div className="tabs" role="tablist" aria-label="Anmelden oder registrieren">
          <button type="button" role="tab" id="tab-login" aria-selected={mode === "login"} onClick={() => { setMode("login"); setError(""); }}>
            Anmelden
          </button>
          <button type="button" role="tab" id="tab-register" aria-selected={mode === "register"} onClick={() => { setMode("register"); setError(""); }}>
            Konto erstellen
          </button>
        </div>
        <form className="stack" onSubmit={submit}>
          <label className="field" htmlFor="email">
            E-Mail-Adresse
            <input type="email" id="email" autoComplete="email" required value={email} onChange={(e) => setEmail(e.target.value)} />
          </label>
          <label className="field" htmlFor="password">
            Passwort{mode === "register" ? " (mindestens 8 Zeichen)" : ""}
            <input
              type="password"
              id="password"
              autoComplete={mode === "login" ? "current-password" : "new-password"}
              required
              minLength={mode === "register" ? 8 : undefined}
              value={password}
              onChange={(e) => setPassword(e.target.value)}
            />
          </label>
          {error && <p className="error" role="alert">{error}</p>}
          <div className="row">
            <button type="submit" className="primary" id="authsubmit" disabled={busy}>
              {busy ? "Bitte warten …" : mode === "login" ? "Anmelden" : "Konto erstellen"}
            </button>
          </div>
          {mode === "register" && (
            <p className="muted">Neue Konten bekommen den Ordner „Einkaufen &amp; Ernährung“ (Englisch, 9 Kapitel) zum Start.</p>
          )}
        </form>
      </section>
    </>
  );
}
