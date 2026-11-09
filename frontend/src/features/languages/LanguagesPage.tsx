import { useState, type FormEvent } from "react";
import { useNavigate } from "react-router";
import { errorMessage } from "../../shared/api/client";
import { PageHeader } from "../../shared/ui/PageHeader";
import { useAsync } from "../../shared/ui/useAsync";
import { BackupCard } from "../backup/BackupCard";
import { languageCode, languagesApi } from "./languagesApi";

export function LanguagesPage() {
  const navigate = useNavigate();
  const { data: languages, error, loading, reload } = useAsync(() => languagesApi.list(), []);
  const [adding, setAdding] = useState(false);
  const [name, setName] = useState("");
  const [formError, setFormError] = useState("");
  const [busy, setBusy] = useState(false);

  async function create(e?: FormEvent) {
    e?.preventDefault();
    const trimmed = name.trim();
    if (!trimmed) return;
    setBusy(true);
    setFormError("");
    try {
      const lang = await languagesApi.create(trimmed);
      navigate(`/languages/${lang.id}`);
    } catch (err) {
      setFormError(errorMessage(err));
      setBusy(false);
    }
  }

  return (
    <>
      <PageHeader title="Vokabeltrainer" subtitle="Wähle eine Sprache" />
      <section className="card" id="langs">
        <div className="label">Sprachen</div>
        <div className="folderlist" id="langlist">
          {loading && !languages && <p className="loading">Wird geladen …</p>}
          {error && <p className="error" role="alert">{error} <button type="button" className="ghost small" onClick={reload}>Nochmals</button></p>}
          {languages?.length === 0 && <p className="muted">Noch keine Sprache. Lege die erste mit „+ Neue Sprache“ an.</p>}
          {languages?.map((lang) => (
            <button type="button" key={lang.id} className="folder lang" id={`lang-${lang.id}`} onClick={() => navigate(`/languages/${lang.id}`)}>
              <span className="lcode" aria-hidden="true">{languageCode(lang.name)}</span>
              <span className="fbody">
                <span className="fname">{lang.name}</span>
                <span className="fmeta">{lang.folderCount} Ordner · {lang.wordCount} Wörter</span>
                {lang.weakCount > 0 && <span className="potchip fchip">{lang.weakCount} nicht gekonnt</span>}
              </span>
            </button>
          ))}
        </div>
        {!adding ? (
          <div className="row" id="newlangrow">
            <button type="button" className="primary" id="newlang" onClick={() => { setAdding(true); setName(""); setFormError(""); }}>
              + Neue Sprache
            </button>
          </div>
        ) : (
          <form className="field" id="langform" onSubmit={create}>
            <label className="field" htmlFor="langname">
              Name der Sprache
              <input type="text" id="langname" placeholder="z. B. Französisch" autoComplete="off" autoFocus value={name} onChange={(e) => setName(e.target.value)} maxLength={60} />
            </label>
            {formError && <p className="error" role="alert">{formError}</p>}
            <div className="row">
              <button type="submit" className="primary" id="langsave" disabled={busy || !name.trim()}>Hinzufügen</button>
              <button type="button" className="ghost" id="langcancel" onClick={() => setAdding(false)}>Abbrechen</button>
            </div>
          </form>
        )}
      </section>
      <BackupCard onImported={reload} />
    </>
  );
}
