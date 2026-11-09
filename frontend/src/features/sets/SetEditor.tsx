import { useMemo, useState, type FormEvent } from "react";
import { useNavigate, useParams } from "react-router";
import { errorMessage } from "../../shared/api/client";
import { PageHeader } from "../../shared/ui/PageHeader";
import { useAsync } from "../../shared/ui/useAsync";
import { foldersApi, type FolderDetail } from "../folders/foldersApi";
import { languagesApi } from "../languages/languagesApi";
import { setsApi, type SetItem } from "./setsApi";

const id = (folderId: string, key: string) => `${folderId}\u0000${key}`;

/** Set neu anlegen (/languages/:languageId/sets/new) oder bearbeiten (/sets/:setId/edit). */
export function SetEditor() {
  const { languageId: languageParam, setId } = useParams();
  const navigate = useNavigate();
  const [name, setName] = useState("");
  const [selected, setSelected] = useState<Set<string>>(new Set());
  const [filter, setFilter] = useState("");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState("");

  const { data, error: loadError, loading } = useAsync(async () => {
    const existing = setId ? await setsApi.get(setId) : null;
    const languageId = existing?.languageId ?? languageParam ?? "";
    const [language, summaries] = await Promise.all([languagesApi.get(languageId), languagesApi.folders(languageId)]);
    const folders = await Promise.all(summaries.map((f) => foldersApi.get(f.id)));
    if (existing) {
      setName(existing.name);
      setSelected(new Set(existing.words.map((w) => id(w.folderId, w.key))));
    }
    return { existing, language, folders };
  }, [setId, languageParam]);

  const needle = filter.trim().toLowerCase();
  const visible = useMemo(() => {
    if (!data) return [];
    return data.folders.map((f) => ({
      folder: f,
      chapters: f.chapters
        .map((c) => ({
          ...c,
          words: needle
            ? c.words.filter((w) => w.german.toLowerCase().includes(needle) || w.answers.some((a) => a.toLowerCase().includes(needle)))
            : c.words,
        }))
        .filter((c) => c.words.length > 0),
    }));
  }, [data, needle]);

  if (loading && !data) return <p className="loading">Wird geladen …</p>;
  if (loadError || !data) return <p className="error" role="alert">{loadError || "Nicht gefunden."}</p>;

  const { existing, language, folders } = data;
  const back = () => navigate(existing ? `/sets/${existing.id}` : `/languages/${language.id}`);

  function toggle(keys: string[], on: boolean) {
    setSelected((prev) => {
      const next = new Set(prev);
      for (const k of keys) {
        if (on) next.add(k);
        else next.delete(k);
      }
      return next;
    });
  }

  /** Reihenfolge wie in den Ordnern. */
  function items(all: FolderDetail[]): SetItem[] {
    return all.flatMap((f) => f.chapters.flatMap((c) => c.words.filter((w) => selected.has(id(f.id, w.key))).map((w) => ({ folderId: f.id, wordKey: w.key }))));
  }

  async function save(e: FormEvent) {
    e.preventDefault();
    setBusy(true);
    setError("");
    try {
      const list = items(folders);
      const saved = existing ? await setsApi.update(existing.id, name, list) : await setsApi.create(language.id, name, list);
      navigate(`/sets/${saved.id}`);
    } catch (err) {
      setError(errorMessage(err));
      setBusy(false);
    }
  }

  return (
    <>
      <PageHeader title={existing ? "Set bearbeiten" : "Neues Set"} subtitle={`${language.name} · Wörter aus allen Ordnern wählen`} />
      <form className="card" id="setform" onSubmit={save}>
        <label className="field" htmlFor="setname">
          Name des Sets
          <input type="text" id="setname" placeholder="z. B. Prüfung Freitag" value={name} maxLength={120} onChange={(e) => setName(e.target.value)} />
        </label>
        <label className="field" htmlFor="setfilter">
          Wörter suchen
          <input type="text" id="setfilter" placeholder="Deutsch oder Fremdsprache …" value={filter} onChange={(e) => setFilter(e.target.value)} autoComplete="off" />
        </label>

        {folders.length === 0 && <p className="muted">In {language.name} gibt es noch keine Ordner.</p>}
        {visible.map(({ folder, chapters }) => (
          <details key={folder.id} className="setfolder" open={visible.length === 1 || !!needle}>
            <summary>{folder.name} <span className="fmeta">· {folder.chapters.reduce((n, c) => n + c.words.filter((w) => selected.has(id(folder.id, w.key))).length, 0)} gewählt</span></summary>
            {chapters.length === 0 && <p className="muted">Keine passenden Wörter.</p>}
            {chapters.map((c, k) => {
              const keys = c.words.map((w) => id(folder.id, w.key));
              const all = keys.every((x) => selected.has(x));
              return (
                <div key={k} className="chapters setchapter">
                  <label className="chapterall">
                    <input type="checkbox" checked={all} onChange={() => toggle(keys, !all)} />
                    <span className="name">{c.name}</span>
                    <span className="pg">{c.words.length}</span>
                  </label>
                  {c.words.map((w) => {
                    const key = id(folder.id, w.key);
                    return (
                      <label key={w.key} className="wordpick">
                        <input type="checkbox" checked={selected.has(key)} onChange={() => toggle([key], !selected.has(key))} />
                        <span className="name">{w.german}</span>
                        <span className="pg">{w.solution}</span>
                      </label>
                    );
                  })}
                </div>
              );
            })}
          </details>
        ))}

        <p className="total" id="settotal">{selected.size} Wörter gewählt</p>
        {error && <p className="error" role="alert">{error}</p>}
        <div className="row">
          <button type="submit" className="primary" id="setsave" disabled={busy || !name.trim() || selected.size === 0}>Speichern</button>
          <button type="button" className="ghost" onClick={back}>Abbrechen</button>
        </div>
      </form>
    </>
  );
}
