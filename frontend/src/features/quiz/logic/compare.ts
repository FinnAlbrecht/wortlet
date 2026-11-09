// Vergleichs- und Diff-Logik, 1:1 aus der HTML-Version übernommen.
// Ob eine Antwort richtig ist, entscheidet der Server (gleiche Regeln). Hier wird erklärt, WAS anders ist.

/** Nur Gross/Klein, Leerzeichen und Apostroph-Art werden angeglichen. Alles andere muss exakt stimmen. */
export const norm = (s: string): string =>
  s.toLowerCase().replace(/[’`´‘]/g, "'").replace(/…/g, "...").replace(/\s+/g, " ").trim();

export const check = (input: string, answers: readonly string[]): boolean =>
  answers.some((a) => norm(a) === norm(input));

const cmpChar = (c: string): string => c.toLowerCase().replace(/[’`´‘]/g, "'");

/** Levenshtein-Distanz der normalisierten Texte. */
export function dist(a: string, b: string): number {
  const A = [...norm(a)];
  const B = [...norm(b)];
  const d: number[][] = Array.from({ length: A.length + 1 }, (_, i) => [i]);
  for (let j = 1; j <= B.length; j++) d[0][j] = j;
  for (let i = 1; i <= A.length; i++)
    for (let j = 1; j <= B.length; j++)
      d[i][j] = Math.min(d[i - 1][j] + 1, d[i][j - 1] + 1, d[i - 1][j - 1] + (A[i - 1] === B[j - 1] ? 0 : 1));
  return d[A.length][B.length];
}

export interface DiffSegment {
  text: string;
  /** Bei „Du“: falsches/überflüssiges Zeichen. Bei „Richtig“: fehlendes/anderes Zeichen. */
  bad: boolean;
}

export interface DiffResult {
  user: DiffSegment[];
  target: DiffSegment[];
  count: number;
}

function merge(chars: Array<[string, boolean]>): DiffSegment[] {
  const out: DiffSegment[] = [];
  for (const [c, bad] of chars) {
    const text = bad && c === " " ? " " : c;
    const last = out[out.length - 1];
    if (last && last.bad === bad) last.text += text;
    else out.push({ text, bad });
  }
  return out;
}

/** Markiert die Unterschiede Zeichen für Zeichen (Du / Richtig). */
export function diffView(user: string, target: string): DiffResult {
  const A = [...user.trim().replace(/\s+/g, " ")];
  const B = [...target];
  const n = A.length;
  const m = B.length;
  const d: number[][] = Array.from({ length: n + 1 }, () => new Array<number>(m + 1).fill(0));
  for (let i = 0; i <= n; i++) d[i][0] = i;
  for (let j = 0; j <= m; j++) d[0][j] = j;
  for (let i = 1; i <= n; i++)
    for (let j = 1; j <= m; j++)
      d[i][j] = Math.min(
        d[i - 1][j] + 1,
        d[i][j - 1] + 1,
        d[i - 1][j - 1] + (cmpChar(A[i - 1]) === cmpChar(B[j - 1]) ? 0 : 1),
      );

  let i = n;
  let j = m;
  const ua: Array<[string, boolean]> = [];
  const ta: Array<[string, boolean]> = [];
  while (i > 0 || j > 0) {
    if (i > 0 && j > 0 && d[i][j] === d[i - 1][j - 1] + (cmpChar(A[i - 1]) === cmpChar(B[j - 1]) ? 0 : 1)) {
      const same = cmpChar(A[i - 1]) === cmpChar(B[j - 1]);
      ua.unshift([A[i - 1], !same]);
      ta.unshift([B[j - 1], !same]);
      i--;
      j--;
    } else if (i > 0 && d[i][j] === d[i - 1][j] + 1) {
      ua.unshift([A[i - 1], true]);
      i--;
    } else {
      ta.unshift([B[j - 1], true]);
      j--;
    }
  }
  return { user: merge(ua), target: merge(ta), count: d[n][m] };
}

/** Kurzer Hinweis, was genau fehlt („Es fehlt „to“ …“). Leer, wenn nichts Typisches erkannt wird. */
export function diffNote(user: string, target: string): string {
  const u = norm(user);
  const t = norm(target);
  const art = t.match(/^(der|die|das|the|le|la|les|l'|el|los|las|il|lo|gli|un|une|ein|eine) ?(.*)$/);
  if (t.startsWith("to ") && u === t.slice(3)) return "Es fehlt „to“ vor dem Verb.";
  if (u.startsWith("to ") && u.slice(3) === t) return "Hier gehört kein „to“ davor.";
  if (art && u === art[2]) return "Es fehlt der Artikel „" + art[1] + "“.";
  if (u.replace(/-/g, " ") === t.replace(/-/g, " ")) return "Achte auf den Bindestrich.";
  if (u.replace(/'/g, "") === t.replace(/'/g, "")) return "Achte auf den Apostroph.";
  if (u.replace(/[.!?…]/g, "").trim() === t.replace(/[.!?…]/g, "").trim()) return "Achte auf die Satzzeichen.";
  if (u.replace(/ /g, "") === t.replace(/ /g, "")) return "Achte auf die Leerzeichen.";
  const c = dist(u, t);
  if (c === 1) return "Nur ein Zeichen ist anders.";
  if (c <= 3) return c + " Zeichen sind anders.";
  return "";
}

/** Die richtige Antwort, die der Eingabe am nächsten kommt (für die Fehleranzeige). */
export function closestAnswer(typed: string, answers: readonly string[]): string {
  return answers.slice().sort((a, b) => dist(typed, a) - dist(typed, b))[0] ?? "";
}

export function shuffle<T>(list: readonly T[], random: () => number = Math.random): T[] {
  const a = list.slice();
  for (let i = a.length - 1; i > 0; i--) {
    const j = Math.floor(random() * (i + 1));
    [a[i], a[j]] = [a[j], a[i]];
  }
  return a;
}

/** Note nach Schweizer Skala aus dem ersten Durchgang (1–6, halbe Noten). */
export function swissGrade(firstRight: number, total: number): number {
  if (total <= 0) return 1;
  return Math.max(1, Math.round((firstRight / total * 5 + 1) * 2) / 2);
}

export const formatGrade = (grade: number): string => grade.toFixed(1).replace(".0", "");
