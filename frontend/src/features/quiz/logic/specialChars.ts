/** Sonderzeichen, die man auf einer Schweizer Tastatur nicht direkt tippen kann – je nach Sprache der Antwort. */

const SETS: Array<{ match: string[]; chars: string[] }> = [
  { match: ["franz", "french", "francais"], chars: ["é", "è", "ê", "ë", "à", "â", "ç", "î", "ï", "ô", "û", "ù", "ü", "œ", "æ", "«", "»"] },
  { match: ["span", "espanol"], chars: ["á", "é", "í", "ó", "ú", "ñ", "ü", "¿", "¡"] },
  { match: ["ital"], chars: ["à", "è", "é", "ì", "ò", "ù"] },
  { match: ["portug"], chars: ["ã", "õ", "á", "â", "à", "é", "ê", "í", "ó", "ô", "ú", "ç"] },
  { match: ["latein", "latin"], chars: ["ā", "ē", "ī", "ō", "ū"] },
  { match: ["turk"], chars: ["ç", "ğ", "ı", "İ", "ö", "ş", "ü"] },
  { match: ["schwed", "swed", "norweg", "norw", "dan"], chars: ["å", "ä", "ö", "æ", "ø"] },
  { match: ["poln", "polish"], chars: ["ą", "ć", "ę", "ł", "ń", "ó", "ś", "ź", "ż"] },
];

/** Englisch braucht keine Leiste. */
const NONE = ["engl"];

/** Für unbekannte Sprachen: die häufigsten Zeichen. */
const FALLBACK = ["á", "à", "â", "é", "è", "ê", "í", "ó", "ô", "ú", "ç", "ñ", "ß"];

/** Deutsch: ß fehlt auf der Schweizer Tastatur, Umlaute als Hilfe auf dem Handy. */
const GERMAN = ["ä", "ö", "ü", "ß"];

const plain = (s: string) => s.normalize("NFD").replace(/[̀-ͯ]/g, "").toLowerCase();

/**
 * Welche Zeichen die Leiste zeigt.
 * @param languageName Name der Sprache, wie der Benutzer ihn eingegeben hat (z. B. „Französisch“).
 * @param toGerman true, wenn die Antwort auf Deutsch ist.
 */
export function specialCharsFor(languageName: string, toGerman: boolean): string[] {
  if (toGerman) return GERMAN;
  const name = plain(languageName);
  if (NONE.some((m) => name.includes(m))) return [];
  if (name.includes("deutsch") || name.includes("german")) return GERMAN;
  return SETS.find((s) => s.match.some((m) => name.includes(m)))?.chars ?? FALLBACK;
}

/** Grossbuchstabe – „ß“ bleibt „ß“ (sonst würde „SS“ daraus). */
export function upper(ch: string): string {
  if (ch === "ß") return ch;
  const u = ch.toLocaleUpperCase("de-CH");
  return u.length === 1 ? u : ch;
}

/** Fügt `ch` an der Cursor-Position ein (ersetzt eine Markierung) und gibt den neuen Text samt Cursor zurück. */
export function insertAt(value: string, start: number | null, end: number | null, ch: string): { value: string; caret: number } {
  const s = Math.max(0, Math.min(start ?? value.length, value.length));
  const e = Math.max(s, Math.min(end ?? s, value.length));
  return { value: value.slice(0, s) + ch + value.slice(e), caret: s + ch.length };
}
