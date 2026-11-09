import { useEffect, useMemo, useState } from "react";
import { useNavigate, useParams } from "react-router";
import { errorMessage } from "../../shared/api/client";
import { ConfirmBox } from "../../shared/ui/ConfirmBox";
import { PageHeader } from "../../shared/ui/PageHeader";
import { languagesApi } from "../languages/languagesApi";
import { PhotoBox } from "../recognition/PhotoBox";
import { foldersApi, type FolderDetail } from "./foldersApi";
import { describeParse, parseText } from "./logic/parseText";

/** Neuen Ordner anlegen (/languages/:languageId/new) oder bestehenden bearbeiten (/folders/:folderId/edit). */
export function FolderEditor() {
  const { languageId: routeLanguageId, folderId } = useParams();
  const navigate = useNavigate();
  const [existing, setExisting] = useState<FolderDetail | null>(null);
  const [languageId, setLanguageId] = useState(routeLanguageId ?? "");
  const [languageName, setLanguageName] = useState("");
  const [name, setName] = useState("");
  const [text, setText] = useState("");
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [saving, setSaving] = useState(false);
  const [confirming, setConfirming] = useState(false);

  useEffect(() => {
    let cancelled = false;
    (async () => {
      try {
        if (folderId) {
          const f = await foldersApi.get(folderId);
          if (cancelled) return;
          setExisting(f);
          setName(f.name);
          setText(f.text);
          setLanguageId(f.languageId);
          setLanguageName(f.languageName);
        } else if (routeLanguageId) {
          const l = await languagesApi.get(routeLanguageId);
          if (!cancelled) setLanguageName(l.name);
        }
      } catch (e) {
        if (!cancelled) setError(errorMessage(e));
      } finally {
        if (!cancelled) setLoading(false);
      }
    })();
    return () => {
      cancelled = true;
    };
  }, [folderId, routeLanguageId]);

  const parsed = useMemo(() => parseText(text), [text]);
  const canSave = parsed.wordCount > 0 && name.trim().length > 0 && !saving;

  function appendLines(lines: string[]) {
    if (!lines.length) return;
    setText((t) => (t.trim() ? t.trimEnd() + "\n" : "") + lines.join("\n"));
  }

  async function save() {
    if (!canSave) return;
    setSaving(true);
    setError("");
    try {
      const saved = existing
        ? await foldersApi.update(existing.id, name.trim(), text.trim())
        : await foldersApi.create(languageId, name.trim(), text.trim());
      navigate(`/folders/${saved.id}`, { replace: true });
    } catch (e) {
      setError(errorMessage(e));
      setSaving(false);
    }
  }

  async function remove() {
    if (!existing) return;
    setSaving(true);
    try {
      await foldersApi.remove(existing.id);
      navigate(`/languages/${existing.languageId}`, { replace: true });
    } catch (e) {
      setError(errorMessage(e));
      setSaving(false);
      setConfirming(false);
    }
  }

  function cancel() {
    if (existing) navigate(`/folders/${existing.id}`);
    else navigate(`/languages/${languageId}`);
  }

  if (loading) return <p className="loading">Wird geladen …</p>;

  return (
    <>
      <PageHeader title={existing ? existing.name : "Neuer Ordner"} subtitle="Wörter eintippen oder aus Fotos erkennen" />
      <section className="card" id="editor">
        <div className="label" id="edlabel">{existing ? "Ordner bearbeiten" : "Neuer Ordner"}</div>
        <label className="field" htmlFor="edname">
          Name des Ordners
          <input type="text" id="edname" placeholder="z. B. Schule und Freizeit" autoComplete="off" maxLength={120} value={name} onChange={(e) => setName(e.target.value)} />
        </label>
        <PhotoBox languageName={languageName} onLines={appendLines} />
        <label className="field" htmlFor="edtext">
          Wörter
          <textarea
            id="edtext"
            rows={12}
            spellCheck={false}
            autoCapitalize="off"
            placeholder={"# Obst\ndie Birne = pear\ndie Kasse = checkout; till"}
            value={text}
            onChange={(e) => setText(e.target.value)}
          />
        </label>
        <p className="muted">
          Eine Zeile pro Wort: <code>Deutsch = {languageName || "Fremdsprache"}</code>. Mehrere richtige Antworten trennst du mit einem Strichpunkt <code>;</code>. Eine Zeile mit{" "}
          <code>#</code> am Anfang beginnt ein neues Kapitel.
        </p>
        <p className="helpline">
          Optional: <code># Kapitel | S. 12 | neu</code> für Seiten und „neue Wörter“, <code>… = … | Hinweis</code> für einen Hinweis bei der Frage.
        </p>
        <p className="total" id="edcount">{describeParse(parsed)}</p>
        {error && <p className="error" role="alert">{error}</p>}
        <div className="row">
          <button type="button" className="primary" id="edsave" disabled={!canSave} onClick={() => void save()}>{saving ? "Speichere …" : "Speichern"}</button>
          <button type="button" className="ghost" id="edcancel" onClick={cancel}>Abbrechen</button>
        </div>
        {existing && !confirming && (
          <div className="row" id="eddelrow">
            <button type="button" className="ghost small danger" id="eddel" onClick={() => setConfirming(true)}>Ordner löschen</button>
          </div>
        )}
        {existing && confirming && (
          <ConfirmBox
            id="edconfirm"
            text={`Ordner „${existing.name}“ mit allen Wörtern wirklich löschen?`}
            busy={saving}
            onConfirm={() => void remove()}
            onCancel={() => setConfirming(false)}
          />
        )}
      </section>
    </>
  );
}
