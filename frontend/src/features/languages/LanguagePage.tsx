import { useState, type FormEvent } from "react";
import { useNavigate, useParams } from "react-router";
import { errorMessage } from "../../shared/api/client";
import { ConfirmBox } from "../../shared/ui/ConfirmBox";
import { PageHeader } from "../../shared/ui/PageHeader";
import { useAsync } from "../../shared/ui/useAsync";
import { languagesApi } from "./languagesApi";

/** Eine Sprache mit ihren Ordnern. */
export function LanguagePage() {
  const { languageId = "" } = useParams();
  const navigate = useNavigate();
  const { data, error, loading, reload } = useAsync(
    async () => {
      const [language, folders] = await Promise.all([languagesApi.get(languageId), languagesApi.folders(languageId)]);
      return { language, folders };
    },
    [languageId],
  );
  const [confirming, setConfirming] = useState(false);
  const [renaming, setRenaming] = useState(false);
  const [newName, setNewName] = useState("");
  const [actionError, setActionError] = useState("");
  const [busy, setBusy] = useState(false);

  if (loading && !data) return <p className="loading">Wird geladen …</p>;
  if (error || !data)
    return (
      <>
        <div className="row"><button type="button" className="ghost small" onClick={() => navigate("/")}>← Alle Sprachen</button></div>
        <p className="error" role="alert">{error || "Diese Sprache gibt es nicht."}</p>
      </>
    );

  const { language, folders } = data;

  async function remove() {
    setBusy(true);
    setActionError("");
    try {
      await languagesApi.remove(language.id);
      navigate("/");
    } catch (err) {
      setActionError(errorMessage(err));
      setConfirming(false);
      setBusy(false);
    }
  }

  async function rename(e: FormEvent) {
    e.preventDefault();
    setBusy(true);
    setActionError("");
    try {
      await languagesApi.rename(language.id, newName);
      setRenaming(false);
      await reload();
    } catch (err) {
      setActionError(errorMessage(err));
    } finally {
      setBusy(false);
    }
  }

  return (
    <>
      <PageHeader title={language.name} subtitle="Wähle einen Ordner" />
      <div className="row" id="langback">
        <button type="button" className="ghost small" id="tolangs" onClick={() => navigate("/")}>← Alle Sprachen</button>
        <button type="button" className="ghost small" id="renamelang" onClick={() => { setRenaming(true); setNewName(language.name); }}>Umbenennen</button>
        {folders.length === 0 && (
          <button type="button" className="ghost small danger" id="dellang" onClick={() => setConfirming(true)}>Sprache löschen</button>
        )}
      </div>
      {renaming && (
        <form className="card" onSubmit={rename}>
          <label className="field" htmlFor="langrename">
            Neuer Name
            <input type="text" id="langrename" autoFocus value={newName} onChange={(e) => setNewName(e.target.value)} maxLength={60} />
          </label>
          <div className="row">
            <button type="submit" className="primary" disabled={busy || !newName.trim()}>Speichern</button>
            <button type="button" className="ghost" onClick={() => setRenaming(false)}>Abbrechen</button>
          </div>
        </form>
      )}
      {confirming && (
        <ConfirmBox id="langconfirm" text={`Sprache „${language.name}“ löschen?`} busy={busy} onConfirm={remove} onCancel={() => setConfirming(false)} />
      )}
      {actionError && <p className="error" role="alert">{actionError}</p>}

      <section className="card" id="folders">
        <div className="label">Deine Ordner</div>
        <div className="folderlist" id="folderlist">
          {folders.length === 0 && (
            <p className="muted">Noch keine Ordner in {language.name}. Erstelle den ersten mit „+ Neuer Ordner“.</p>
          )}
          {folders.map((f) => (
            <button type="button" key={f.id} className="folder" id={`folder-${f.id}`} onClick={() => navigate(`/folders/${f.id}`)}>
              <span className="ficon" aria-hidden="true" />
              <span className="fbody">
                <span className="fname">{f.name}</span>
                <span className="fmeta">{f.description} · {f.chapterCount} Kapitel · {f.wordCount} Wörter</span>
                {f.weakCount > 0 && <span className="potchip fchip">{f.weakCount} nicht gekonnt</span>}
              </span>
            </button>
          ))}
        </div>
        <div className="row">
          <button type="button" className="primary" id="newfolder" onClick={() => navigate(`/languages/${language.id}/new`)}>+ Neuer Ordner</button>
        </div>
      </section>
    </>
  );
}
