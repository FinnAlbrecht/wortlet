import { useEffect, useMemo, useState } from "react";
import { useNavigate, useParams } from "react-router";
import { PageHeader } from "../../shared/ui/PageHeader";
import { useAsync } from "../../shared/ui/useAsync";
import { readDirection, saveDirection, type Direction } from "../quiz/direction";
import { ExamSetup } from "../quiz/ExamSetup";
import { QuizRunner } from "../quiz/QuizRunner";
import { WeakWordsCard } from "../weak-words/WeakWordsCard";
import { foldersApi, type Word } from "./foldersApi";

const RULES_BOOK =
  "Gross- und Kleinschreibung ist egal. Sonst muss alles stimmen: „to“ bei Verben, Apostroph (z. B. florist's), Bindestrich und „sth.“ / „sb.“ wie im Buch. Wo das Buch britisches (BE) und amerikanisches (AE) Englisch angibt, zählt beides.";
const RULES_CUSTOM = "Gross- und Kleinschreibung ist egal. Sonst muss alles genau so stimmen, wie es im Ordner eingetragen ist.";

interface Session {
  id: number;
  words: Word[];
  practiceMode: boolean;
}

/** Ein Ordner: Menü (Fehlerwörter + Prüfung einrichten) und die Prüfung selbst. */
export function FolderPage() {
  const { folderId = "" } = useParams();
  const navigate = useNavigate();
  const { data: folder, error, loading, reload } = useAsync(() => foldersApi.get(folderId), [folderId]);
  const [selected, setSelected] = useState<Set<number>>(new Set());
  const [direction, setDirection] = useState<Direction>(readDirection);
  const [session, setSession] = useState<Session | null>(null);

  // Beim ersten Laden alle Kapitel auswählen.
  const chapterCount = folder?.chapters.length ?? 0;
  useEffect(() => {
    setSelected(new Set(Array.from({ length: chapterCount }, (_, k) => k)));
  }, [folderId, chapterCount]);

  const byKey = useMemo(() => new Map((folder?.chapters ?? []).flatMap((c) => c.words).map((w) => [w.key, w])), [folder]);
  const weakWords = useMemo(() => (folder?.weakWords ?? []).map((k) => byKey.get(k)).filter((w): w is Word => !!w), [folder, byKey]);

  if (loading && !folder) return <p className="loading">Wird geladen …</p>;
  if (error || !folder)
    return (
      <>
        <div className="row"><button type="button" className="ghost small" onClick={() => navigate("/")}>← Alle Sprachen</button></div>
        <p className="error" role="alert">{error || "Diesen Ordner gibt es nicht."}</p>
      </>
    );

  const subtitle = `${folder.description} · Deutsch ↔ ${folder.languageName}${folder.isSeed ? " · genau wie im Buch" : ""}`;

  function start(words: Word[], practiceMode: boolean) {
    if (!words.length) return;
    setSession({ id: Date.now(), words, practiceMode });
    window.scrollTo(0, 0);
  }

  function toMenu() {
    setSession(null);
    void reload();
    window.scrollTo(0, 0);
  }

  return (
    <>
      <PageHeader title={folder.name} subtitle={subtitle} />
      {session ? (
        <QuizRunner
          key={session.id}
          folder={folder}
          words={session.words}
          practiceMode={session.practiceMode}
          direction={direction}
          initialWeakCount={folder.weakWords.length}
          onMenu={toMenu}
        />
      ) : (
        <>
          <div className="row" id="backrow">
            <button type="button" className="ghost small" id="back" onClick={() => navigate(`/languages/${folder.languageId}`)}>← Alle Ordner</button>
            <button type="button" className="ghost small" id="editfolder" onClick={() => navigate(`/folders/${folder.id}/edit`)}>Ordner bearbeiten</button>
          </div>
          <WeakWordsCard words={weakWords} onPractice={() => start(weakWords, true)} />
          <ExamSetup
            languageName={folder.languageName}
            chapters={folder.chapters}
            oldLabel={folder.oldLabel}
            newLabel={folder.newLabel}
            selected={selected}
            onSelectedChange={setSelected}
            direction={direction}
            onDirectionChange={(d) => { setDirection(d); saveDirection(d); }}
            onStart={() => start(folder.chapters.flatMap((c, k) => (selected.has(k) ? c.words : [])), false)}
          />
          <p className="muted" id="rules">{folder.isSeed ? RULES_BOOK : RULES_CUSTOM}</p>
        </>
      )}
    </>
  );
}
