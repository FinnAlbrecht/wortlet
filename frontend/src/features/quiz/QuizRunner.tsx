import { useEffect, useRef, useState, type FormEvent } from "react";
import { errorMessage } from "../../shared/api/client";
import type { Word } from "../folders/foldersApi";
import { statsApi } from "../stats/statsApi";
import type { Direction } from "./direction";
import { closestAnswer, diffNote, diffView, formatGrade, shuffle, swissGrade, type DiffSegment } from "./logic/compare";
import { quizApi } from "./quizApi";

type Phase = "question" | "answered" | "pot" | "result";

type Feedback =
  | { kind: "ok"; text: string; small?: string }
  | {
      kind: "bad";
      title: string;
      small?: string;
      diff?: { user: DiffSegment[]; target: DiffSegment[] };
      note?: string;
      canOverride: boolean;
      wasWeakBefore: boolean;
      solution: string;
    };

/** Ein Wort mit dem Ordner, zu dem es gehört – dorthin gehen die Antworten (auch bei Sets). */
export interface QuizWord extends Word {
  folderId: string;
}

interface Props {
  languageName: string;
  words: QuizWord[];
  practiceMode: boolean;
  direction: Direction;
  /** Was geprüft wird – für den Prüfungsverlauf in der Statistik. */
  source: { folderId: string } | { setId: string };
  /** Anzahl „Wörter, die ich nicht kann“ im Ordner; null bei Sets (die Liste gehört zu den Ordnern). */
  initialWeakCount: number | null;
  onMenu: () => void;
}

function Segments({ list, cls }: { list: DiffSegment[]; cls: string }) {
  return (
    <>
      {list.map((s, k) => (s.bad ? <span key={k} className={cls}>{s.text}</span> : <span key={k}>{s.text}</span>))}
    </>
  );
}

/** Prüfung mit Topf: so viele Durchgänge, bis alle Wörter richtig sind. Note aus dem ersten Durchgang. */
export function QuizRunner({ languageName, words, practiceMode, direction, source, initialWeakCount, onMenu }: Props) {
  const lang = languageName;
  const [round, setRound] = useState(1);
  const [deck, setDeck] = useState<QuizWord[]>([]);
  const [dirs, setDirs] = useState<Array<"de" | "fx">>([]);
  const [i, setI] = useState(0);
  const [pot, setPot] = useState<QuizWord[]>([]);
  const [firstRight, setFirstRight] = useState(0);
  const [phase, setPhase] = useState<Phase>("question");
  const [input, setInput] = useState("");
  const [feedback, setFeedback] = useState<Feedback | null>(null);
  const [pending, setPending] = useState(false);
  const [error, setError] = useState("");
  const [weakCount, setWeakCount] = useState<number | null>(initialWeakCount);
  const total = words.length;
  const answerRef = useRef<HTMLInputElement>(null);
  const goRef = useRef<HTMLButtonElement>(null);
  const potGoRef = useRef<HTMLButtonElement>(null);

  function runRound(list: QuizWord[]) {
    const d = shuffle(list);
    setDeck(d);
    setDirs(d.map(() => (direction === "mix" ? (Math.random() < 0.5 ? "de" : "fx") : direction)));
    setI(0);
    setInput("");
    setFeedback(null);
    setError("");
    setPhase("question");
  }

  // Start
  useEffect(() => {
    runRound(words);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  useEffect(() => {
    if (phase === "question") answerRef.current?.focus();
    else if (phase === "answered") goRef.current?.focus();
    else if (phase === "pot") potGoRef.current?.focus();
  }, [phase, i, round]);

  const word = deck[i];
  const reverse = dirs[i] === "fx";

  async function judge(skip: boolean) {
    if (!word || pending) return;
    const typed = input;
    setPending(true);
    setError("");
    try {
      const r = await quizApi.answer(word.folderId, {
        wordKey: word.key,
        direction: reverse ? "toGerman" : "toForeign",
        input: typed,
        skipped: skip,
        round,
        practiceMode,
      });
      setWeakCount((c) => (c === null ? null : r.weakCount));
      if (r.correct) {
        if (round === 1) setFirstRight((x) => x + 1);
        setFeedback({
          kind: "ok",
          text: "Richtig!" + (practiceMode && round === 1 ? " Raus aus der Liste." : ""),
          small: r.acceptedAnswers.length > 1 ? "Im Buch: " + r.solution : undefined,
        });
      } else {
        setPot((p) => [...p, word]);
        if (skip || !typed.trim()) {
          setFeedback({
            kind: "bad",
            title: (skip ? "Lösung" : "Falsch") + " – kommt in den Topf",
            small: "Richtig: " + r.solution,
            canOverride: !skip,
            wasWeakBefore: r.wasWeakBefore,
            solution: r.solution,
          });
        } else {
          const target = closestAnswer(typed, r.acceptedAnswers);
          const dv = diffView(typed, target);
          setFeedback({
            kind: "bad",
            title: "Falsch – kommt in den Topf",
            diff: { user: dv.user, target: dv.target },
            note: diffNote(typed, target) || undefined,
            small: r.acceptedAnswers.length > 1 ? "Alle richtigen Antworten: " + r.solution : undefined,
            canOverride: true,
            wasWeakBefore: r.wasWeakBefore,
            solution: r.solution,
          });
        }
      }
      setPhase("answered");
    } catch (e) {
      setError(errorMessage(e) + " Versuche es nochmals.");
    } finally {
      setPending(false);
    }
  }

  async function overrideAnswer() {
    if (!word || feedback?.kind !== "bad") return;
    const { wasWeakBefore, solution } = feedback;
    setPot((p) => {
      const k = p.lastIndexOf(word);
      if (k < 0) return p;
      const copy = p.slice();
      copy.splice(k, 1);
      return copy;
    });
    if (round === 1) setFirstRight((x) => x + 1);
    setFeedback({
      kind: "ok",
      text: "Als richtig gewertet." + (practiceMode && round === 1 ? " Raus aus der Liste." : ""),
      small: "Im Buch: " + solution,
    });
    goRef.current?.focus();
    try {
      const r = await quizApi.override(word.folderId, { wordKey: word.key, round, practiceMode, wasWeakBefore });
      setWeakCount((c) => (c === null ? null : r.weakCount));
    } catch (e) {
      setError(errorMessage(e));
    }
  }

  function next() {
    if (i + 1 >= deck.length) {
      // Nach dem ersten Durchgang steht die Note fest → im Prüfungsverlauf speichern.
      if (round === 1)
        void statsApi
          .saveResult({ ...source, direction, practiceMode, wordCount: total, firstRoundCorrect: firstRight })
          .catch(() => {
            // Die Prüfung selbst geht auch ohne Verlaufseintrag weiter.
          });
      setPhase(pot.length ? "pot" : "result");
    } else {
      setI(i + 1);
      setInput("");
      setFeedback(null);
      setError("");
      setPhase("question");
    }
  }

  function submit(e: FormEvent) {
    e.preventDefault();
    if (phase === "answered") next();
    else void judge(false);
  }

  function workPot() {
    const list = pot;
    setPot([]);
    setRound((r) => r + 1);
    runRound(list);
  }

  if (!word && phase !== "result") return <p className="loading">Wird vorbereitet …</p>;

  const progress = phase === "pot" || phase === "result" ? 100 : (i / Math.max(1, deck.length)) * 100;
  const pct = Math.round((firstRight / total) * 100);
  const grade = formatGrade(swissGrade(firstRight, total));

  return (
    <>
      {phase !== "result" && (
        <div className="bar" id="bar">
          <span className={"mode" + (round > 1 ? " pot" : "")} id="mode">
            {(practiceMode ? "Nicht gekonnt · " : "") + (round === 1 ? "Durchgang 1" : "Topf · Runde " + round)}
          </span>
          <div className="track"><div className="fill" id="fill" style={{ width: progress + "%" }} /></div>
          <span id="count">{Math.min(i + 1, deck.length)} / {deck.length}</span>
          <span className="potchip" id="potchip">Topf: {pot.length}</span>
          <button type="button" className="ghost small" id="menu" onClick={onMenu}>Zum Menü</button>
        </div>
      )}

      {(phase === "question" || phase === "answered") && word && (
        <section className="card" id="quiz">
          <div className="label" id="qlabel">{reverse ? "Auf Deutsch übersetzen" : `Auf ${lang} übersetzen`}</div>
          <p className="word" id="word">{reverse ? word.solution : word.german}</p>
          {!reverse && word.hint && <p className="hint" id="hint">{word.hint}</p>}
          <form id="form" autoComplete="off" onSubmit={submit}>
            <div style={{ display: "flex", flexDirection: "column", gap: 12 }}>
              <input
                type="text"
                id="answer"
                ref={answerRef}
                placeholder="Genau wie im Buch …"
                autoCapitalize="off"
                autoCorrect="off"
                spellCheck={false}
                value={input}
                disabled={phase === "answered" || pending}
                onChange={(e) => setInput(e.target.value)}
                aria-label="Deine Antwort"
              />
              <div className="row">
                <button type="submit" className="primary" id="go" ref={goRef} disabled={pending}>
                  {phase === "answered" ? (i === deck.length - 1 ? "Durchgang beenden" : "Weiter") : pending ? "Prüfe …" : "Prüfen"}
                </button>
                {phase === "question" && (
                  <button type="button" className="ghost" id="skip" disabled={pending} onClick={() => void judge(true)}>Weiß ich nicht</button>
                )}
              </div>
            </div>
          </form>
          {error && <p className="error" role="alert">{error}</p>}
          {feedback && (
            <div id="fb" className={"fb " + (feedback.kind === "ok" ? "ok" : "bad")} aria-live="polite">
              {feedback.kind === "ok" ? (
                <>
                  {feedback.text}
                  {feedback.small && <small>{feedback.small}</small>}
                </>
              ) : (
                <>
                  {feedback.title}
                  {feedback.diff && (
                    <div className="diff">
                      <span className="k">Du</span>
                      <span className="v"><Segments list={feedback.diff.user} cls="dx-bad" /></span>
                      <span className="k">Richtig</span>
                      <span className="v"><Segments list={feedback.diff.target} cls="dx-miss" /></span>
                    </div>
                  )}
                  {feedback.note && <span className="diffnote">{feedback.note}</span>}
                  {feedback.small && <small>{feedback.small}</small>}
                  {feedback.canOverride && (
                    <button type="button" className="ghost small override" id="override" onClick={() => void overrideAnswer()}>
                      War doch richtig
                    </button>
                  )}
                </>
              )}
            </div>
          )}
        </section>
      )}

      {phase === "pot" && (
        <section className="card" id="potcard">
          <div className="label">Durchgang beendet</div>
          <p className="pothead" id="pothead">{pot.length + (pot.length === 1 ? " Wort" : " Wörter")} im Topf</p>
          <p className="muted">Diese Wörter musst du nochmals machen, bis alle richtig sind. Was du wieder falsch hast, kommt zurück in den Topf.</p>
          <div className="list" id="potlist">
            {pot.map((w, k) => (
              <div key={k}><span>{w.key}</span><span className="right">{w.solution}</span></div>
            ))}
          </div>
          <div className="row"><button type="button" className="primary" id="potgo" ref={potGoRef} onClick={workPot}>Topf durcharbeiten</button></div>
        </section>
      )}

      {phase === "result" && (
        <section className="card" id="result">
          <div className="label">Geschafft – alle Wörter richtig</div>
          <p className="score" id="score">{firstRight} / {total}</p>
          <p className="grade" id="grade">Im ersten Durchgang: {pct} % richtig · Note {grade}</p>
          <p className="muted" id="rounds">
            {(round === 1 ? "Kein einziges Wort im Topf. Stark!" : "Du hast den Topf in " + (round - 1) + (round === 2 ? " Runde" : " Runden") + " leer gemacht.") +
              (weakCount === null ? "" : " In „Wörter, die ich nicht kann“ sind jetzt " + weakCount + (weakCount === 1 ? " Wort." : " Wörter."))}
          </p>
          <div className="row"><button type="button" className="primary" id="restart" onClick={onMenu}>Neue Prüfung</button></div>
        </section>
      )}
    </>
  );
}
