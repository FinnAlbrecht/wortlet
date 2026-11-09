namespace Wortlet.Application.Recognition;

public static class RecognitionPrompt
{
    /// <summary>Prompt aus PHOTO_PROMPT der HTML-Version.</summary>
    public static string ForPhoto(string languageName)
    {
        var lang = string.IsNullOrWhiteSpace(languageName) ? "Englisch" : languageName.Trim();
        return $$"""
            Das Bild zeigt eine Seite aus einem {{lang}}-Deutsch-Vokabelbuch. Lies ALLE Vokabeleinträge heraus, in der Reihenfolge wie im Buch.
            Antworte nur mit JSON in dieser Form:
            {"chapters":[{"name":"Kapitelüberschrift oder leerer Text","words":[{"de":"deutscher Eintrag","en":["englischer Eintrag"]}]}]}
            Regeln:
            - "de": der deutsche Eintrag genau wie gedruckt, mit Artikel (z. B. "die Birne", "das Obst, die Frucht").
            - "en": der {{lang}}e Eintrag (auch wenn die Sprache nicht Englisch ist, heisst das Feld "en") genau wie gedruckt, mit Artikel, Bindestrichen, Apostrophen, Akzenten und Abkürzungen genau so (bei Englisch Verben mit "to"). Gibt es mehrere Varianten (z. B. britisch/amerikanisch), jede als eigenen Eintrag in der Liste.
            - Lautschrift, Wortart-Angaben (n, v, adj, phrase), Beispielsätze und Hinweiskästen weglassen.
            - Einträge, die nur teilweise im Bild zu sehen sind, weglassen.
            - Ein Kapitel nur mit Namen angeben, wenn auf dieser Seite eine Überschrift steht; sonst "name" leer lassen.
            """;
    }
}
