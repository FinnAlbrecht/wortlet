import type { Chapter } from "../folders/foldersApi";
import type { Direction } from "./direction";

interface Props {
  languageName: string;
  chapters: Chapter[];
  oldLabel: string;
  newLabel: string;
  selected: Set<number>;
  onSelectedChange: (s: Set<number>) => void;
  direction: Direction;
  onDirectionChange: (d: Direction) => void;
  onStart: () => void;
}

/** Richtung und Kapitelauswahl (Alle / Alte / Neue / Keine). Die Reihenfolge ist immer gemischt. */
export function ExamSetup(p: Props) {
  const total = p.chapters.reduce((n, c, k) => n + (p.selected.has(k) ? c.words.length : 0), 0);
  const hasNew = p.chapters.some((c) => c.isNew);
  const hasOld = p.chapters.some((c) => !c.isNew);
  const select = (pred: (c: Chapter) => boolean) =>
    p.onSelectedChange(new Set(p.chapters.flatMap((c, k) => (pred(c) ? [k] : []))));
  const toggle = (k: number) => {
    const next = new Set(p.selected);
    if (next.has(k)) next.delete(k);
    else next.add(k);
    p.onSelectedChange(next);
  };

  const dirs: Array<[Direction, string]> = [
    ["de", `Deutsch → ${p.languageName}`],
    ["fx", `${p.languageName} → Deutsch`],
    ["mix", "Gemischt"],
  ];

  return (
    <section className="card" id="setup">
      <div className="label">Richtung</div>
      <div className="seg" role="radiogroup" aria-label="Richtung">
        {dirs.map(([value, label]) => (
          <label key={value}>
            <input type="radio" name="dir" id={`dir-${value}`} value={value} checked={p.direction === value} onChange={() => p.onDirectionChange(value)} />
            <span>{label}</span>
          </label>
        ))}
      </div>
      <div className="label">Welche Kapitel willst du üben?</div>
      <div className="row">
        <button type="button" className="ghost small" id="all" onClick={() => select(() => true)}>Alle auswählen</button>
        {hasNew && hasOld && (
          <>
            <button type="button" className="ghost small" id="oldonly" onClick={() => select((c) => !c.isNew)}>{p.oldLabel}</button>
            <button type="button" className="ghost small" id="newonly" onClick={() => select((c) => c.isNew)}>{p.newLabel}</button>
          </>
        )}
        <button type="button" className="ghost small" id="none" onClick={() => select(() => false)}>Keine</button>
      </div>
      <div className="chapters" id="chapters">
        {p.chapters.map((c, k) => (
          <label key={k}>
            <input type="checkbox" id={`ch${k}`} checked={p.selected.has(k)} onChange={() => toggle(k)} />
            <span className="name">
              {c.name}
              {c.isNew && <span className="new">neu</span>}
            </span>
            <span className="pg">{c.pages ? `${c.pages} · ` : ""}{c.words.length}</span>
          </label>
        ))}
      </div>
      <p className="total" id="total">{total} Wörter ausgewählt</p>
      <div className="row">
        <button type="button" className="primary" id="begin" disabled={total === 0} onClick={p.onStart}>Prüfung starten</button>
      </div>
    </section>
  );
}
