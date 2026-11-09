import { useState } from "react";
import { useNavigate, useParams } from "react-router";
import { errorMessage } from "../../shared/api/client";
import { ConfirmBox } from "../../shared/ui/ConfirmBox";
import { PageHeader } from "../../shared/ui/PageHeader";
import { useAsync } from "../../shared/ui/useAsync";
import { readDirection, saveDirection, type Direction } from "../quiz/direction";
import { QuizRunner, type QuizWord } from "../quiz/QuizRunner";
import { setsApi } from "./setsApi";

interface Session {
  id: number;
  words: QuizWord[];
  practiceMode: boolean;
}

/** Ein Set: Wörter ansehen und üben. Die Antworten zählen für die Ordner, aus denen die Wörter stammen. */
export function SetPage() {
  const { setId = "" } = useParams();
  const navigate = useNavigate();
  const { data: set, error, loading, reload } = useAsync(() => setsApi.get(setId), [setId]);
  const [direction, setDirection] = useState<Direction>(readDirection);
  const [session, setSession] = useState<Session | null>(null);
  const [confirming, setConfirming] = useState(false);
  const [actionError, setActionError] = useState("");

  if (loading && !set) return <p className="loading">Wird geladen …</p>;
  if (error || !set)
    return (
      <>
        <div className="row"><button type="button" className="ghost small" onClick={() => navigate("/")}>← Alle Sprachen</button></div>
        <p className="error" role="alert">{error || "Dieses Set gibt es nicht."}</p>
      </>
    );

  const folderCount = new Set(set.words.map((w) => w.folderId)).size;
  const weak = set.words.filter((w) => w.isWeak);

  function start(words: QuizWord[], practiceMode: boolean) {
    if (!words.length) return;
    setSession({ id: Date.now(), words, practiceMode });
    window.scrollTo(0, 0);
  }

  async function remove() {
    try {
      await setsApi.remove(set!.id);
      navigate(`/languages/${set!.languageId}`);
    } catch (e) {
      setActionError(errorMessage(e));
      setConfirming(false);
    }
  }

  const dirs: Array<[Direction, string]> = [
    ["de", `Deutsch → ${set.languageName}`],
    ["fx", `${set.languageName} → Deutsch`],
    ["mix", "Gemischt"],
  ];

  return (
    <>
      <PageHeader title={set.name} subtitle={`Set · ${set.words.length} Wörter aus ${folderCount} ${folderCount === 1 ? "Ordner" : "Ordnern"}`} />
      {session ? (
        <QuizRunner
          key={session.id}
          languageName={set.languageName}
          words={session.words}
          practiceMode={session.practiceMode}
          direction={direction}
          source={{ setId: set.id }}
          initialWeakCount={null}
          onMenu={() => { setSession(null); void reload(); window.scrollTo(0, 0); }}
        />
      ) : (
        <>
          <div className="row">
            <button type="button" className="ghost small" onClick={() => navigate(`/languages/${set.languageId}`)}>← {set.languageName}</button>
            <button type="button" className="ghost small" id="editset" onClick={() => navigate(`/sets/${set.id}/edit`)}>Set bearbeiten</button>
            <button type="button" className="ghost small danger" id="delset" onClick={() => setConfirming(true)}>Set löschen</button>
          </div>
          {confirming && (
            <ConfirmBox text={`Set „${set.name}“ löschen? Die Wörter bleiben in ihren Ordnern.`} onConfirm={() => void remove()} onCancel={() => setConfirming(false)} />
          )}
          {actionError && <p className="error" role="alert">{actionError}</p>}

          <section className="card" id="setsetup">
            <div className="label">Richtung</div>
            <div className="seg" role="radiogroup" aria-label="Richtung">
              {dirs.map(([value, label]) => (
                <label key={value}>
                  <input type="radio" name="dir" value={value} checked={direction === value} onChange={() => { setDirection(value); saveDirection(value); }} />
                  <span>{label}</span>
                </label>
              ))}
            </div>
            {set.words.length === 0 ? (
              <p className="muted">Dieses Set ist leer – die Wörter wurden in den Ordnern gelöscht oder umbenannt. Bearbeite das Set, um neue zu wählen.</p>
            ) : (
              <div className="row">
                <button type="button" className="primary" id="setbegin" onClick={() => start(set.words, false)}>Alle {set.words.length} Wörter prüfen</button>
                {weak.length > 0 && (
                  <button type="button" className="primary potbtn" id="setweak" onClick={() => start(weak, true)}>{weak.length} nicht gekonnte üben</button>
                )}
              </div>
            )}
            <details>
              <summary>Wörter ansehen</summary>
              <div className="list">
                {set.words.map((w) => (
                  <div key={w.folderId + w.key}>
                    <span>{w.key}{w.isWeak && <span className="potchip small">nicht gekonnt</span>}</span>
                    <span className="right">{w.solution}</span>
                  </div>
                ))}
              </div>
            </details>
          </section>
        </>
      )}
    </>
  );
}
