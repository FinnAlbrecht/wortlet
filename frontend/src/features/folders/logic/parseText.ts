// Textformat des Wörterfelds – gleiche Regeln wie im Backend (VocabularyText.Parse).
//   # Kapitel                         neues Kapitel
//   Deutsch = Antwort; Alternative    ein Wort, Alternativen mit „;“
// Optional: „# Kapitel | S. 12 | neu“ und „Deutsch = Antwort | Hinweis“.

export interface ParsedWord {
  key: string;
  answers: string[];
  hint?: string;
}

export interface ParsedChapter {
  name: string;
  pages: string;
  isNew: boolean;
  words: ParsedWord[];
}

export interface ParseResult {
  chapters: ParsedChapter[];
  /** Zeilennummern (ab 1) ohne „=“ oder ohne Inhalt. */
  errors: number[];
  wordCount: number;
}

export const DEFAULT_CHAPTER = "Alle Wörter";

export function parseText(text: string): ParseResult {
  const chapters: ParsedChapter[] = [];
  const errors: number[] = [];
  const seen = new Set<string>();
  let cur: ParsedChapter | null = null;

  String(text ?? "").replace(/\r\n/g, "\n").split("\n").forEach((raw, n) => {
    const line = raw.trim();
    if (!line) return;
    if (line.startsWith("#")) {
      const parts = line.replace(/^#+/, "").split("|").map((p) => p.trim());
      const extra = parts.slice(1);
      const isNew = extra.some((p) => p.toLowerCase() === "neu");
      const pages = extra.find((p) => p && p.toLowerCase() !== "neu") ?? "";
      cur = { name: parts[0] || "Kapitel", pages, isNew, words: [] };
      chapters.push(cur);
      return;
    }
    const eq = line.indexOf("=");
    if (eq < 0) {
      errors.push(n + 1);
      return;
    }
    const de = line.slice(0, eq).trim();
    let rest = line.slice(eq + 1);
    let hint: string | undefined;
    const bar = rest.indexOf("|");
    if (bar >= 0) {
      hint = rest.slice(bar + 1).trim() || undefined;
      rest = rest.slice(0, bar);
    }
    const answers = rest.split(";").map((x) => x.trim()).filter(Boolean);
    if (!de || !answers.length) {
      errors.push(n + 1);
      return;
    }
    let key = de;
    let k = 2;
    while (seen.has(key)) key = de + " (" + k++ + ")";
    seen.add(key);
    if (!cur) {
      cur = { name: DEFAULT_CHAPTER, pages: "", isNew: false, words: [] };
      chapters.push(cur);
    }
    cur.words.push(hint ? { key, answers, hint } : { key, answers });
  });

  const nonEmpty = chapters.filter((c) => c.words.length);
  return { chapters: nonEmpty, errors, wordCount: nonEmpty.reduce((a, c) => a + c.words.length, 0) };
}

/** „12 Wörter in 2 Kapiteln · Zeile 3 hat kein „=“ und wird ignoriert“ – wie im Original. */
export function describeParse(result: ParseResult): string {
  const n = result.wordCount;
  const c = result.chapters.length;
  const { errors } = result;
  return (
    n + (n === 1 ? " Wort" : " Wörter") + " in " + c + (c === 1 ? " Kapitel" : " Kapiteln") +
    (errors.length
      ? " · Zeile " + errors.slice(0, 5).join(", ") + (errors.length > 5 ? " …" : "") + " hat kein „=“ und wird ignoriert"
      : "")
  );
}
