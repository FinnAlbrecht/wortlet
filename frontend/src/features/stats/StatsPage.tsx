import { useNavigate } from "react-router";
import { PageHeader } from "../../shared/ui/PageHeader";
import { useAsync } from "../../shared/ui/useAsync";
import { formatGrade } from "../quiz/logic/compare";
import { statsApi, type DayStat } from "./statsApi";

const DIRECTION: Record<string, string> = { de: "Deutsch → Fremdsprache", fx: "Fremdsprache → Deutsch", mix: "Gemischt" };
const WEEKDAY = ["So", "Mo", "Di", "Mi", "Do", "Fr", "Sa"];

const dateFmt = new Intl.DateTimeFormat("de-CH", { day: "numeric", month: "numeric", year: "2-digit", hour: "2-digit", minute: "2-digit" });

function plural(n: number, one: string, many: string) {
  return `${n} ${n === 1 ? one : many}`;
}

/** Antworten der letzten 4 Wochen als Säulen (richtig dunkel, falsch hell). */
function ActivityChart({ days }: { days: DayStat[] }) {
  const max = Math.max(1, ...days.map((d) => d.answers));
  return (
    <div className="activity" role="img" aria-label="Antworten pro Tag in den letzten 4 Wochen">
      {days.map((d) => {
        const date = new Date(d.day + "T12:00:00");
        const label = `${WEEKDAY[date.getDay()]} ${date.getDate()}.${date.getMonth() + 1}.: ${plural(d.answers, "Antwort", "Antworten")}, ${d.correct} richtig`;
        return (
          <div key={d.day} className="day" title={label}>
            <div className="col" style={{ height: `${(d.answers / max) * 100}%` }}>
              <div className="wrong" style={{ flexGrow: d.answers - d.correct }} />
              <div className="right" style={{ flexGrow: d.correct }} />
            </div>
          </div>
        );
      })}
    </div>
  );
}

export function StatsPage() {
  const navigate = useNavigate();
  const { data, error, loading, reload } = useAsync(async () => {
    const [overview, results, hardest] = await Promise.all([statsApi.overview(), statsApi.results(30), statsApi.hardest(undefined, 10)]);
    return { overview, results, hardest };
  }, []);

  if (loading && !data) return <p className="loading">Wird geladen …</p>;
  if (error || !data)
    return <p className="error" role="alert">{error} <button type="button" className="ghost small" onClick={reload}>Nochmals</button></p>;

  const { overview: o, results, hardest } = data;
  const accuracy = o.totalAnswers ? Math.round((o.correctAnswers / o.totalAnswers) * 100) : null;
  const tiles: Array<[string, string, string?]> = [
    ["Lernserie", plural(o.currentStreak, "Tag", "Tage"), `Rekord: ${plural(o.longestStreak, "Tag", "Tage")}`],
    ["Heute", plural(o.todayAnswers, "Antwort", "Antworten")],
    ["Diese Woche", plural(o.weekAnswers, "Antwort", "Antworten")],
    ["Richtig", accuracy === null ? "–" : `${accuracy} %`, `von ${o.totalAnswers} Antworten`],
    ["Geübte Wörter", String(o.wordsPracticed)],
    ["Prüfungen", String(o.quizzes), o.averageGrade !== null ? `Ø Note ${formatGrade(o.averageGrade)}` : undefined],
  ];

  return (
    <>
      <PageHeader title="Statistik" subtitle="Dein Lernstand über alle Sprachen" />
      <section className="card" id="statsoverview">
        <div className="tiles">
          {tiles.map(([label, value, sub]) => (
            <div key={label} className="tile">
              <span className="label">{label}</span>
              <span className="tvalue">{value}</span>
              {sub && <span className="fmeta">{sub}</span>}
            </div>
          ))}
        </div>
        <div className="label">Letzte 4 Wochen</div>
        <ActivityChart days={o.lastDays} />
        <p className="muted"><span className="dot right" /> richtig <span className="dot wrong" /> falsch</p>
      </section>

      <section className="card" id="hardest">
        <div className="label">Die schwierigsten Wörter</div>
        {hardest.length === 0 ? (
          <p className="muted">Noch keine Fehler – oder noch nicht geübt.</p>
        ) : (
          <div className="list">
            {hardest.map((h) => (
              <div key={h.folderId + h.german}>
                <span>{h.german}<small className="fmeta"> · {h.folderName}</small></span>
                <span className="right">{h.solution} <small className="fmeta">({h.wrong} von {h.attempts} falsch)</small></span>
              </div>
            ))}
          </div>
        )}
      </section>

      <section className="card" id="history">
        <div className="label">Prüfungsverlauf</div>
        {results.length === 0 ? (
          <p className="muted">Noch keine Prüfung abgeschlossen. Nach dem ersten Durchgang wird das Ergebnis hier gespeichert.</p>
        ) : (
          <div className="results">
            {results.map((r) => {
              const target = r.folderId ? `/folders/${r.folderId}` : r.setId ? `/sets/${r.setId}` : null;
              return (
                <button type="button" key={r.id} className="result" disabled={!target} onClick={() => target && navigate(target)}>
                  <span className="fbody">
                    <span className="rtitle">{r.title}</span>
                    <span className="fmeta">{dateFmt.format(new Date(r.finishedAt))} · {DIRECTION[r.direction] ?? r.direction} · {r.firstRoundCorrect} / {r.wordCount} richtig</span>
                  </span>
                  <span className={"rgrade" + (r.grade >= 4 ? " good" : " low")}>{formatGrade(r.grade)}</span>
                </button>
              );
            })}
          </div>
        )}
      </section>
    </>
  );
}
