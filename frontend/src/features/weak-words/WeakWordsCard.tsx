import type { Word } from "../folders/foldersApi";

/** „Wörter, die ich nicht kann“ – pro Ordner, im Konto gespeichert. */
export function WeakWordsCard({ words, onPractice }: { words: Word[]; onPractice: () => void }) {
  const n = words.length;
  return (
    <section className="card weakcard" id="weakcard">
      <div className="label">Wörter, die ich nicht kann</div>
      <p className="pothead" id="weakhead">{n === 0 ? "Keine Wörter – super!" : n + (n === 1 ? " Wort" : " Wörter")}</p>
      <p className="muted">
        Jedes Wort, das du irgendwann falsch hast, kommt hier hinein. Ein Wort kommt nur wieder raus, wenn du es hier beim ersten Versuch richtig hast.
        Auch hier musst du alle Wörter richtig haben, bis der Topf leer ist.
      </p>
      {n > 0 && (
        <details id="weakdetails">
          <summary>Liste ansehen</summary>
          <div className="list" id="weaklist">
            {words.map((w) => (
              <div key={w.key}><span>{w.key}</span><span className="right">{w.solution}</span></div>
            ))}
          </div>
        </details>
      )}
      <div className="row">
        <button type="button" className="primary potbtn" id="weakgo" disabled={n === 0} onClick={onPractice}>Diese Wörter üben</button>
      </div>
      <p className="muted" id="savenote">Gespeichert in deinem Konto, auch auf anderen Geräten verfügbar.</p>
    </section>
  );
}
