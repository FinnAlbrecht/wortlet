/** Notlösung ohne Server-Schlüssel: Prompt für einen beliebigen KI-Chat. Die Antwort kommt direkt im Format „Deutsch = Fremdsprache“. */
export function buildChatPrompt(languageName: string): string {
  const lang = languageName.trim() || "Englisch";
  return [
    `Ich schicke dir ein Foto (oder mehrere) von Seiten aus einem ${lang}-Deutsch-Vokabelbuch. Lies ALLE Vokabeleinträge heraus, in der Reihenfolge wie im Buch.`,
    "",
    "Antworte NUR mit den Wörtern – ohne Einleitung, ohne Erklärungen, ohne Markdown oder Codeblock. Eine Zeile pro Eintrag, genau in diesem Format:",
    `deutscher Eintrag = Eintrag auf ${lang}`,
    "",
    "Regeln:",
    `- Links Deutsch, rechts ${lang}, getrennt durch „ = “.`,
    "- Deutsch genau wie gedruckt, mit Artikel (z. B. „die Birne“, „das Obst, die Frucht“).",
    `- ${lang} genau wie gedruckt, mit Artikel, Bindestrichen, Apostrophen, Akzenten und Abkürzungen (bei Englisch Verben mit „to“).`,
    "- Gibt es mehrere Varianten (z. B. britisch/amerikanisch), trenne sie rechts mit Strichpunkt: die Kasse = checkout; till",
    "- Steht auf der Seite eine Kapitelüberschrift, schreibe davor eine eigene Zeile: # Überschrift",
    "- Lautschrift, Wortart-Angaben (n, v, adj), Beispielsätze und Hinweiskästen weglassen.",
    "- Einträge, die nur teilweise zu sehen sind, weglassen.",
    "- Kein „=“ im deutschen Teil, keine Nummern, keine Aufzählungszeichen.",
    "",
    "Beispiel:",
    "# Obst",
    "die Birne = pear",
    "die Kasse = checkout; till",
  ].join("\n");
}

/** Text in die Zwischenablage – mit Ausweichweg für Browser ohne Clipboard-API (z. B. http im WLAN). */
export async function copyToClipboard(text: string): Promise<boolean> {
  try {
    if (navigator.clipboard && window.isSecureContext) {
      await navigator.clipboard.writeText(text);
      return true;
    }
  } catch {
    // weiter mit dem Ausweichweg
  }
  try {
    const ta = document.createElement("textarea");
    ta.value = text;
    ta.setAttribute("readonly", "");
    ta.style.position = "fixed";
    ta.style.opacity = "0";
    document.body.appendChild(ta);
    ta.select();
    const ok = document.execCommand("copy");
    document.body.removeChild(ta);
    return ok;
  } catch {
    return false;
  }
}
