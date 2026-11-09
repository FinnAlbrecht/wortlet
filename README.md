# Wortlet – Vokabeltrainer

Ein Vokabeltrainer für Schule und Selbststudium: Wörter eintippen oder aus Buchfotos erkennen lassen, dann üben, bis alles sitzt. Mit Benutzerkonto, damit dein Stand auf Handy und Computer derselbe ist.

Backend: ASP.NET Core 10 (C#, Entity Framework Core, SQLite/PostgreSQL, Identity + JWT) in DDD-Schichten.
Frontend: React + TypeScript + Vite, nach Features gegliedert.

---

## Was die App kann

- **Sprachen → Ordner → Übersicht.** Eigene Sprachen und Ordner erstellen, umbenennen/bearbeiten und löschen. Eine Sprache lässt sich nur löschen, wenn sie leer ist.
- **Wörter eintippen** im Format `Deutsch = Fremdsprache`, Alternativen mit `;`, Kapitel mit `#`:
  ```
  # Obst
  die Birne = pear
  die Kasse = checkout; cash desk; till
  ```
  Optional: `# Obst | S. 216–217 | neu` (Seiten, „neue Wörter“) und `die Kasse = checkout; till | eines reicht` (Hinweis bei der Frage).
- **Prüfung einrichten:** Kapitel wählen (Alle / Alte / Neue / Keine), Richtung (Deutsch → Fremdsprache, Fremdsprache → Deutsch, Gemischt). Die Reihenfolge ist immer gemischt.
- **Strenger Vergleich:** Nur Gross/Klein (sowie Leerzeichen und Apostroph-Art) ist egal. In der Richtung → Deutsch gilt: Artikel nötig, ein Teil von „das Obst, die Frucht“ reicht, Klammern sind optional, und alle deutschen Bedeutungen zählen (cream = die Sahne / der Rahm).
- **Topf:** Falsche Wörter kommen in den Topf, der in weiteren Runden durchgearbeitet wird, bis alles richtig ist.
- **Fehleranzeige** mit markierten Unterschieden (Du / Richtig) und Hinweisen wie „Es fehlt „to“ vor dem Verb.“, „Es fehlt der Artikel „die“.“, „Achte auf den Bindestrich.“
- **„War doch richtig“**, **„Zum Menü“** und eine **Note nach Schweizer Skala** aus dem ersten Durchgang.
- **„Wörter, die ich nicht kann“** pro Ordner: Ein Wort kommt hinein, wenn es irgendwann falsch ist, und nur heraus, wenn es im Übungsmodus im ersten Durchgang richtig ist.
- **Bilderkennung über OpenRouter:** Buchseiten fotografieren, mehrere Fotos nacheinander mit Fortschrittsanzeige. Die erkannten Wörter landen als Zeilen im Wörterfeld, damit du sie vor dem Speichern prüfen kannst. Limit pro Benutzer und Tag. Notlösung: **„Prompt kopieren“** für einen beliebigen KI-Chat.
- **Daten sichern:** „Daten exportieren“ (JSON mit allen Sprachen, Ordnern und Fehlerwörtern) und „Daten importieren“ (mit Rückfrage vor dem Überschreiben).
- **Startdaten:** Jedes neue Konto bekommt den Ordner „Einkaufen & Ernährung“ (Englisch, 9 Kapitel, 319 Wörter, mit „Alte Wörter (S. 201–215)“ / „Neue Wörter (S. 216–225)“).
- **Profil:** Name und Avatar – ein Symbol auf Farbe oder ein eigenes Foto (wird im Browser quadratisch auf 256 px verkleinert; sehen können es nur Mitglieder der eigenen Gruppen).
- **Sets:** Eigene Wortauswahl innerhalb einer Sprache, auch über mehrere Ordner (z. B. „Prüfung Freitag“). Sets verweisen auf die Wörter im Ordner: Korrekturen wirken sofort, gelöschte Wörter fallen weg. Antworten zählen für die Fehlerliste des jeweiligen Ordners.
- **Statistik:** Lernserie (Tage in Folge), Antworten heute/diese Woche, Trefferquote, Aktivität der letzten 4 Wochen, die schwierigsten Wörter und der Prüfungsverlauf mit Note (gespeichert nach dem ersten Durchgang). Tage zählen nach Schweizer Zeit.
- **Gruppen:** Jeder kann eine Gruppe erstellen (wird Leitung). Drei Arten: **Privat** (Standard; nicht in der Suche, nur per Code), **Auf Anfrage** (in der Namenssuche; die Leitung nimmt Anfragen an oder lehnt ab) und **Öffentlich** (in der Namenssuche; direkt beitreten). Mit dem 8-stelligen Code tritt man jeder Gruppe direkt bei. Mitglieder sehen eine Wochen-Rangliste (Name, Avatar, geübte Wörter, Lernserie – keine E-Mail-Adressen). Die Leitung teilt Ordner und Sets; Mitglieder übernehmen sie als eigene Kopie und können sie später aktualisieren.
- Karopapier-Design, Hell-/Dunkelmodus (automatisch oder per Knopf), handytauglich.

---

## Feature-Liste (Roadmap)

Alle geplanten und fertigen Funktionen, nach Bereichen geordnet.

- [x] fertig
- [ ] offen – *teilweise* heisst: ein Teil ist schon umgesetzt
- 💎 könnte später Teil eines kostenpflichtigen Angebots werden (siehe Bereich 12)

### 1. Benutzer und Admin

**Konten**

- [x] **Anmeldung:** Jeder Benutzer hat eigene Sprachen, Ordner, Sets, Fehlerlisten und Statistiken.
- [x] **Profil:** Name und Avatar (Symbol auf Farbe oder eigenes Foto), dazu Klassen bzw. Gruppen.
- [ ] **Passwort vergessen:** Zurücksetzen per E-Mail-Link.
- [ ] **E-Mail bestätigen** bei der Registrierung.
- [ ] **Konto löschen:** Mit allen Daten, direkt im Profil (Datenschutz).
- [ ] **Anmelden mit Google / Microsoft** (Schulkonten).
- [ ] **Rollen:** Schüler, Lehrperson, Admin.
- [ ] **Elternansicht:** Eltern sehen den Lernstand ihres Kindes (nur mit dessen Zustimmung).

**Admin-Ansicht** 💎

- [ ] **Übersicht über alle Benutzer:** Wann zuletzt geübt, Anzahl Wörter, Durchschnittsnote und Anzahl Fehlerwörter.
- [ ] **Detailansicht pro Benutzer:** Ordner, Notenverlauf, schwierigste Wörter und Fehlerliste.
- [ ] **Zugriff auf Lernsets:** Der Admin kann Sets eines Benutzers öffnen, kopieren und bearbeiten.

**Aufgaben vom Admin („Du musst das lernen“)** 💎

- [ ] **Set zuweisen:** An einen oder mehrere Benutzer, mit Fälligkeitsdatum und Mindestnote (z. B. „bis Freitag, mindestens Note 5“).
- [ ] **Bereich „Aufgaben“ beim Benutzer:** Mit Countdown und Status (offen, begonnen, erledigt).
- [ ] **Kontrolle für den Admin:** Wer die Aufgabe erledigt hat und mit welcher Note.

**Datenschutz**

- [ ] **Transparenz:** Jeder Benutzer sieht, was der Admin von ihm sehen kann. *Teilweise:* Beim Beitritt zu einer Gruppe steht, was die anderen Mitglieder sehen.
- [ ] **Privat oder öffentlich:** Eigene Sets können privat oder öffentlich sein. *Teilweise:* Gruppen sind privat, auf Anfrage oder öffentlich.
- [x] **Keine E-Mail-Adressen sichtbar:** Andere sehen nur Name und Avatar. Das Profilbild sehen nur Mitglieder der eigenen Gruppen.
- [ ] **Datenschutzerklärung und Impressum** in der App.

### 2. Sets, Organisation und Teilen

- [x] **Sprachen → Ordner → Kapitel:** Eigene Sprachen und Ordner, Kapitel mit Seitenangaben und „alte / neue Wörter“.
- [x] **Sets:** Eigene Wortauswahl über mehrere Ordner einer Sprache (z. B. „Prüfung Freitag“).
- [ ] **Tags:** Sets und Wörter nach Fach, Thema, Wortart oder Prüfung ordnen.
- [ ] **Favoriten:** Wörter mit Stern markieren und als eigene Liste üben.
- [ ] **Suche über alle eigenen Wörter:** Quer durch alle Ordner.
- [ ] **Suche über alle öffentlichen Sets:** Nach Sprache, Thema, Buch oder Kapitel.
- [ ] **Übernehmen:** Ein gefundenes Set mit einem Klick in die eigenen Ordner kopieren. *Teilweise:* In Gruppen geteilte Ordner und Sets lassen sich schon übernehmen und später aktualisieren.
- [ ] **Bewertung und Beliebtheit:** Wie oft ein Set übernommen wurde.
- [ ] **Melden:** Unpassende öffentliche Sets melden, der Admin prüft sie.
- [ ] **Sets teilen per Link:** Andere können ein Set öffnen und kopieren.
- [x] **Klassen und Gruppen:** Gruppen erstellen, mit Code beitreten oder per Namen suchen (privat / auf Anfrage / öffentlich). Die Leitung teilt Ordner und Sets mit allen Mitgliedern.
- [ ] **Gemeinsam bearbeiten:** Mehrere Personen erstellen zusammen ein Set.
- [x] **Wörter eintippen:** Im Format `Deutsch = Fremdsprache`, mit Alternativen und Kapiteln.
- [ ] **Import:** Sets aus Quizlet, Anki oder CSV/Excel importieren.
- [ ] **Export:** Als CSV (Sicherung, Austausch) oder PDF (zum Ausdrucken). PDF ist 💎.
- [x] **Alle Daten exportieren und importieren:** Als JSON-Datei.
- [x] **Automatische Sicherungen** der Datenbank, auch vor jedem Update.
- [ ] **Papierkorb:** Gelöschte Ordner und Sets 30 Tage lang wiederherstellen.

### 3. Karten-Inhalte

- [x] **Hinweis pro Wort:** z. B. „eines reicht“, wird bei der Frage angezeigt.
- [ ] **Bilder auf Karten:** Selbst hochladen, ideal für Biologie, Geografie und zum besseren Merken. 💎 ab einer gewissen Speichermenge.
- [ ] **Formeln (LaTeX):** Mathe- und Physikformeln schön darstellen.
- [ ] **Audio auf Karten:** Aussprache aufnehmen oder einfügen. 💎
- [ ] **Beispielsätze:** Pro Wort ein oder mehrere Sätze (auch per KI, siehe Bereich 8).
- [ ] **Wortart und Grammatik:** Geschlecht, Plural, unregelmässige Verben mit Stammformen.

### 4. Lern- und Spielmodi

**Klassische Lernmodi**

- [ ] **Karteikarten:** Karte umdrehen und selbst bewerten, auf dem Handy mit Wischen.
- [x] **Schreib-Modus:** Antwort selbst eintippen.
- [ ] **Multiple Choice:** Vier Antworten, eine davon richtig.
- [ ] **Zuordnen:** Zwei Spalten, die passenden Paare verbinden, auf Zeit.
- [ ] **Lückentext:** Beispielsatz mit Lücke für das richtige Wort.
- [ ] **Hören und Diktat:** Die Karte wird vorgelesen (Text-to-Speech). Im Diktat schreibst du das gehörte Wort auf.
- [ ] **Sprechen:** Antwort laut sagen, die Spracherkennung prüft sie.
- [ ] **Prüfungsmodus / Test-Generator:** Probeprüfung aus gemischten Fragetypen, ohne Tipps und ohne „Weiss ich nicht“, mit Note am Schluss. *Teilweise:* Prüfung im Schreib-Modus mit Schweizer Note.
- [ ] **Verb-Trainer:** Konjugationen üben (je, tu, il … / I, you, he …).

**Spiele**

- [ ] **Tippspiel gegen die Zeit:** Zu langsam oder falsch kostet ein Leben (3 Leben). Mit jeder richtigen Antwort wird es schneller. Highscore am Ende.
- [ ] **60-Sekunden-Sprint:** So viele Karten wie möglich in einer Minute.
- [ ] **Memory:** Karten aufdecken und Paare finden, mit Zeit- und Zug-Zähler.
- [ ] **Wörter-Regen:** Wörter fallen von oben und müssen übersetzt werden, bevor sie unten ankommen.
- [ ] **Galgenmännchen:** Die Übersetzung Buchstabe für Buchstabe erraten.
- [ ] **Buchstabensalat:** Gemischte Buchstaben der Lösung ordnen.
- [ ] **Duell:** Live gegen Freunde dasselbe Set spielen – wer zuerst richtig antwortet, gewinnt.
- [ ] **Gruppen-Challenge:** Die ganze Gruppe spielt eine Woche lang gegen ein gemeinsames Ziel.

**Endlos-Modus (Zufallsvoci)**

- [ ] **Zufällige Wörter aus der aktuellen Sprache:** Aus allen Ordnern dieser Sprache, so viele du willst.
- [ ] **Quelle wählbar:** Nur eigene Wörter, nur Fehlerwörter oder auch öffentliche Sets anderer.
- [ ] **Serie und Rekord:** z. B. „37 richtig am Stück“.

### 5. Abfrage und Korrektur

- [x] **Richtung wählen:** Deutsch → Fremdsprache, umgekehrt oder gemischt.
- [x] **Kapitel wählen:** Alle, nur alte, nur neue oder einzelne Kapitel.
- [ ] **Anzahl wählen:** z. B. 20 zufällige Karten.
- [x] **Genaue Antwortprüfung:** Gross-/Kleinschreibung ist egal. Bei „→ Deutsch“ zählen Artikel, Teilbedeutungen und optionale Klammern.
- [ ] **Unscharfe Antwortprüfung:** Kleine Tippfehler oder Synonyme werden automatisch als richtig gewertet (abschaltbar).
- [x] **„War doch richtig“-Knopf:** Für Tippfehler oder gültige Alternativen, die nicht erkannt wurden.
- [x] **Fehler anzeigen:** Markierte Unterschiede und Hinweise wie „Es fehlt der Artikel „die“.“
- [ ] **Tipp-Knopf:** Erster Buchstabe oder Anzahl Buchstaben.
- [ ] **Sonderzeichen-Leiste:** é, è, à, ç, ñ, ü und ähnliche Zeichen.
- [ ] **Vorlesen:** Lautsprecher-Knopf für die Aussprache.
- [x] **Topf / Fehler-Modus:** Falsche Wörter kommen in den Topf und werden wiederholt, bis alles sitzt. Dazu kommt „Wörter, die ich nicht kann“ pro Ordner.
- [ ] **Fehlerliste von Hand bearbeiten:** Wörter selbst hinzufügen oder entfernen.

### 6. Klüger lernen

- [ ] **Spaced Repetition (Lernkartei):** Karten kommen dann wieder, wenn du sie fast vergessen hast – einfach (nach 1, 3, 7 und 14 Tagen) oder mit dem SM-2-Algorithmus wie bei Anki.
- [ ] **Schwierigkeits-Bewertung:** Nach jeder Karte „leicht / mittel / schwer“ wählen. Die Wahl beeinflusst die Wiederholung.
- [ ] **Lernfortschritt pro Karte:** Status „neu → lernend → sicher → gemeistert“.
- [ ] **Prüfungsdatum mit Lernplan:** Die Karten werden auf die Tage bis zur Prüfung verteilt, mit Prognose („Wenn du täglich Y lernst, kannst du bis zum X alles“). 💎

### 7. Motivation

- [ ] **Tagesziel:** z. B. 20 Karten pro Tag, mit Fortschrittsbalken.
- [x] **Lernserie (Streak):** Tage am Stück geübt.
- [ ] **Punkte (XP) und Levels:** Punkte für jede richtige Antwort, Level-Aufstiege.
- [ ] **Abzeichen:** z. B. „100 Karten gemeistert“, „7 Tage Streak“ oder „Topf in 1 Runde leer“.
- [ ] **Ranglisten:** Wöchentlich unter Freunden, pro Klasse oder Gruppe, auf Wunsch anonym oder ausschaltbar. *Teilweise:* Wochen-Rangliste pro Gruppe gibt es schon. Anonym und ausschaltbar fehlen noch.
- [ ] **Konfetti:** Bei leerem Topf oder einer 6.
- [ ] **Wort des Tages:** Auf dem Startbildschirm.
- [ ] **Serien-Schutz:** Einen verpassten Tag „retten“. 💎

### 8. KI-Funktionen

- [x] **Wörter aus Buchfotos erkennen:** Mehrere Fotos nacheinander, mit Prüfen vor dem Speichern. Gratis mit Tageslimit, ohne Limit 💎.
- [x] **„Prompt kopieren“** als Notlösung für einen beliebigen KI-Chat.
- [ ] **KI-Kartengenerator:** Notizen, Text oder PDF hochladen, daraus werden automatisch Lernkarten erstellt. 💎
- [ ] **Automatische Falschantworten:** Plausible Antwortmöglichkeiten für Multiple Choice.
- [ ] **Erklär-Button:** Bei einer falschen Antwort erklärt die KI, warum die richtige Antwort stimmt. 💎
- [ ] **Beispielsätze und Eselsbrücken:** Auf Knopfdruck für schwierige Karten. 💎
- [ ] **Neue Wörter entdecken:** Passende neue Wörter vorschlagen, die man zu einem Ordner hinzufügen kann. 💎
- [ ] **KI-Gesprächspartner:** Kurze Unterhaltung in der Fremdsprache mit den Wörtern aus dem Ordner. 💎

### 9. Statistik und Übersicht

- [ ] **Lernstatistik:** Grafiken zu Lernzeit, Trefferquote und Fortschritt. *Teilweise:* Trefferquote, Antworten heute/diese Woche und Aktivität der letzten 4 Wochen gibt es schon. Die Lernzeit fehlt noch.
- [x] **Statistik pro Karte:** Wie oft richtig und falsch, dazu eine Liste der schwierigsten Wörter.
- [ ] **Notenverlauf als Diagramm pro Ordner.** *Teilweise:* Den Prüfungsverlauf mit Noten gibt es schon als Liste.
- [ ] **Kalender-Heatmap:** Wie bei GitHub, an welchen Tagen du gelernt hast. *Teilweise:* Die Aktivität der letzten 4 Wochen wird schon angezeigt.
- [x] **Note nach Schweizer Skala** aus dem ersten Durchgang.
- [ ] **Wochenbericht:** Zusammenfassung per E-Mail. 💎

### 10. Komfort und Praktisches

- [ ] **Erinnerungen / Push-Nachrichten:** „Du hast heute noch 15 Karten fällig“, „Du hast heute noch nicht geübt“ oder „Aufgabe morgen fällig“.
- [ ] **Offline üben:** Lernen ohne Internet, die Ergebnisse werden später synchronisiert. 💎
- [ ] **Mobile App / PWA:** Auf dem Handy wie eine echte App installierbar.
- [x] **Handytauglich:** Die Oberfläche passt sich dem Bildschirm an.
- [x] **Dark Mode:** Automatisch oder per Knopf.
- [ ] **Tastaturkürzel:** Schnell lernen mit Leertaste, Pfeiltasten usw.
- [ ] **Einstellungen:** Schriftgrösse, Zeit im Tippspiel, ob Gross- und Kleinschreibung zählt.
- [ ] **Eigene Designs / Farbthemen.** 💎
- [ ] **Browser-Erweiterung:** Markierte Wörter auf einer Webseite direkt als Karte speichern.
- [ ] **Oberfläche in mehreren Sprachen** (Deutsch, Französisch, Italienisch, Englisch).
- [ ] **Barrierefreiheit:** Bildschirmleser, hoher Kontrast, Legasthenie-Schrift.

### 11. Technik und Betrieb

- [x] **Datenbank mit Migrationen:** SQLite oder PostgreSQL, Updates ohne Datenverlust.
- [x] **Automatische Tests:** Unit-, Integrations- und End-to-End-Tests auf GitHub.
- [x] **Docker-Container** zum einfachen Online-Stellen.
- [ ] **Online gestellt** (z. B. Fly.io) mit eigener Domain.
- [ ] **Sicherere Anmeldung:** Token im HttpOnly-Cookie statt im Browser-Speicher.
- [ ] **Sicherungen ausser Haus:** Automatisch in einen Cloud-Speicher kopieren.
- [ ] **Fehler-Überwachung und Nutzungsstatistik** (datenschutzfreundlich, ohne Tracking durch Dritte).

### 12. Premium-Ideen (später evtl. kostenpflichtig) 💎

Die Grundfunktionen zum Lernen sollen gratis bleiben. Kosten können vor allem dort aufkommen, wo die App selbst Geld kostet: KI-Anfragen, Speicher, Server.

- [ ] **Wortlet Plus (Einzelperson):** Alle KI-Funktionen ohne Tageslimit, Bilder und Audio auf Karten, Offline-Modus, PDF-Export, Serien-Schutz, eigene Designs, Wochenbericht.
- [ ] **Wortlet für Schulen (Lizenz pro Klasse oder Schule):** Admin- und Lehrpersonen-Ansicht, Aufgaben zuweisen mit Kontrolle, Anmelden mit Schulkonto, Datenexport für Noten.
- [ ] **Familien-Abo:** Mehrere Kinder plus Elternansicht.
- [ ] **Bezahlung:** z. B. Stripe oder TWINT, Testphase gratis, Rabatt für Schüler.
- [ ] **Fertige Lehrmittel-Sets:** Wortlisten zu bekannten Schulbüchern, nur mit Erlaubnis der Verlage.

---

## Lokal starten

### Variante 1: Mit Docker (ein Befehl)

Voraussetzung: [Docker Desktop](https://www.docker.com/products/docker-desktop/).

```bash
cp .env.example .env      # optional: OpenRouter-Schlüssel eintragen
docker compose up --build
```

Dann im Browser **http://localhost:8080** öffnen und ein Konto erstellen.
Die Daten (SQLite-Datenbank) liegen im Docker-Volume `wortlet-data` und bleiben beim Neustart erhalten. Beenden mit `Ctrl+C` bzw. `docker compose down`.

### Variante 2: Ohne Docker

Voraussetzungen: [.NET 10 SDK](https://dotnet.microsoft.com/download) und [Node.js 22](https://nodejs.org/).

Terminal 1 – Backend (läuft auf http://localhost:5080, legt die Datenbank in `backend/src/Wortlet.Api/data/` an):

```bash
cd backend
dotnet run --project src/Wortlet.Api
```

Terminal 2 – Frontend (läuft auf http://localhost:5173 und leitet `/api` ans Backend weiter):

```bash
cd frontend
npm install
npm run dev
```

Dann **http://localhost:5173** öffnen.
Mit dem Handy im gleichen WLAN: `npm run dev -- --host` und auf dem Handy `http://<IP-deines-Computers>:5173` öffnen.

---

## OpenRouter-Schlüssel einrichten (Bilderkennung)

Der Schlüssel liegt **nur auf dem Server** – nie im Frontend, nie im Repository, nie in `appsettings.json`.

1. Bei [openrouter.ai](https://openrouter.ai/) anmelden und unter **Keys** einen Schlüssel erstellen.
2. **Wichtig: Für den Schlüssel ein Ausgabelimit (Credit limit) setzen**, z. B. 1 $. So kann auch bei einem Fehler nie mehr verbraucht werden.
3. Schlüssel hinterlegen:

**Entwicklung (dotnet run) – User Secrets:**

```bash
cd backend/src/Wortlet.Api
dotnet user-secrets set "OpenRouter:ApiKey" "sk-or-v1-…"
```

**Produktion / Docker – Umgebungsvariable:**

```bash
OpenRouter__ApiKey=sk-or-v1-…
```

Bei Docker Compose trägst du ihn in `.env` als `OPENROUTER_API_KEY=…` ein.

### Modell und Limits

| Einstellung | Standard | Bedeutung |
|---|---|---|
| `OpenRouter:Model` | `google/gemma-4-31b-it:free` | Erstes Modell |
| `OpenRouter:FallbackModels` | `google/gemma-4-26b-a4b-it:free,nvidia/nemotron-3.5-super-vl:free,openrouter/free` | Werden der Reihe nach versucht, wenn ein Modell nicht verfügbar ist |
| `Recognition:DailyLimitPerUser` | `30` | Fotos pro Benutzer und Tag |
| `OpenRouter:Referer` | `http://localhost:8080` | Wird als `HTTP-Referer` geschickt – auf die öffentliche Adresse setzen |

Das Gratis-Kontingent von OpenRouter gilt für das ganze Konto (50 Anfragen pro Tag, mit mindestens 10 gekauften Credits 1000). Darum gibt es zusätzlich das Limit pro Benutzer. Ist etwas aufgebraucht oder kein Modell verfügbar, zeigt die App eine verständliche Meldung auf Deutsch – „Prompt kopieren“ funktioniert immer.

Als Umgebungsvariable schreibt man `:` als `__`, also z. B. `OpenRouter__Model` oder `Recognition__DailyLimitPerUser`.

---

## Weitere Einstellungen

| Einstellung | Bedeutung |
|---|---|
| `Jwt:Key` / `Jwt__Key` | Geheimer Schlüssel für die Anmelde-Tokens (mind. 32 Zeichen, z. B. `openssl rand -base64 48`). Fehlt er, wird einer erzeugt und im Datenordner gespeichert. In Produktion setzen. |
| `Storage:DataDirectory` | Ordner für SQLite-Datei und erzeugten Schlüssel (Docker: `/data`) |
| `Database:Provider` | `Sqlite` (Standard) oder `Postgres` |
| `ConnectionStrings:Default` | Bei PostgreSQL z. B. `Host=…;Database=wortlet;Username=…;Password=…` |

**PostgreSQL:** Das Datenmodell hat eigene Migrationen für SQLite und PostgreSQL (`backend/src/Wortlet.Infrastructure/Persistence/Migrations/`). Zum Wechseln nur `Database__Provider=Postgres` und den Connection String setzen; die Tabellen werden beim Start angelegt.

**Neue Migration** nach einer Änderung am Modell:

```bash
cd backend
dotnet tool install --global dotnet-ef
dotnet ef migrations add NameDerAenderung --project src/Wortlet.Infrastructure --startup-project src/Wortlet.Api --context SqliteAppDbContext --output-dir Persistence/Migrations/Sqlite
dotnet ef migrations add NameDerAenderung --project src/Wortlet.Infrastructure --startup-project src/Wortlet.Api --context PostgresAppDbContext --output-dir Persistence/Migrations/Postgres
```

(Alternativ auf GitHub unter **Actions → „EF-Core-Migrationen erzeugen“**.)

---

## Daten und Sicherungen

Alle Daten (Konten, Sprachen, Ordner, Wörter, Fehlerwörter) liegen in einer SQLite-Datenbank `wortlet.db` im Datenordner (lokal `backend/src/Wortlet.Api/data/`, in Docker das Volume `wortlet-data`). Passwörter werden nur als Hash gespeichert (ASP.NET Identity, PBKDF2).

**Datenbank-Updates:** Änderungen am Datenmodell laufen über EF-Core-Migrationen und werden beim Start automatisch eingespielt. Datenbanken aus Versionen vor den Migrationen werden erkannt und übernommen, ohne Daten zu verlieren.

**Automatische Sicherungen:** Die App kopiert die Datenbank beim Start (falls die letzte Sicherung älter als das Intervall ist) und danach alle 24 Stunden – im laufenden Betrieb und immer in sich stimmig (SQLite-Online-Backup). Zusätzlich wird vor jedem Update, das die Tabellen ändert, eine Sicherung `…-vor-update.db` angelegt. Die neuesten 14 Sicherungen bleiben liegen.

| Einstellung | Standard | Bedeutung |
|---|---|---|
| `Backup:Enabled` | `true` | Sicherungen ein/aus |
| `Backup:IntervalHours` | `24` | Abstand zwischen zwei Sicherungen |
| `Backup:KeepCount` | `14` | Anzahl Sicherungen, die behalten werden |
| `Backup:Directory` | `backups` im Datenordner (Docker: `/backups`) | Zielordner – am besten ein anderes Laufwerk bzw. Volume als die Daten |

Bei PostgreSQL ist die automatische Sicherung aus; dort die Backups des Datenbank-Anbieters verwenden.

**Wiederherstellen:**

1. App stoppen.
2. Im Datenordner `wortlet.db` umbenennen (z. B. in `wortlet.db.alt`) und `wortlet.db-wal` / `wortlet.db-shm` löschen, falls vorhanden.
3. Die gewünschte Sicherung (z. B. `backups/wortlet-20261108-224327.db`) als `wortlet.db` in den Datenordner kopieren.
4. App starten.

Docker: Sicherung aus dem Volume holen mit `docker compose cp app:/backups ./backups`.
Für echte Ausfallsicherheit die Sicherungen zusätzlich regelmässig an einen anderen Ort kopieren (anderer Rechner, Cloud-Speicher) – Sicherungen auf demselben Server helfen nicht, wenn der ganze Server verloren geht.

---

## Online stellen

Die App ist ein einziger Docker-Container (Backend liefert das Frontend mit aus) und braucht einen **dauerhaften Speicher** für die SQLite-Datei – oder eine PostgreSQL-Datenbank.
Gibt es beim Anbieter nur ein Volume an `/data`, zusätzlich `Backup__Directory=/data/backups` setzen, damit die automatischen Sicherungen erhalten bleiben.

| Anbieter | Geeignet, weil | Hinweis |
|---|---|---|
| **Fly.io** | Docker + Volume, Server in Europa (z. B. Frankfurt) | Anleitung unten |
| **Railway** | Docker + Volume, sehr einfach per GitHub | Volume an `/data` hängen |
| **Render** | Docker, GitHub-Anbindung | Für SQLite eine „Persistent Disk“ an `/data` (kostenpflichtiger Plan) oder Render-PostgreSQL verwenden |
| **Azure App Service** | Offiziell für .NET, Linux-Container | Speicher an `/data` einbinden oder Azure Database for PostgreSQL |

Preise ändern sich – bitte auf der Website des Anbieters prüfen.

### Kurzanleitung: Fly.io

1. [flyctl installieren](https://fly.io/docs/flyctl/install/) und `fly auth login`.
2. Im Repository-Ordner:
   ```bash
   fly launch --no-deploy        # erkennt das Dockerfile; Region z. B. fra wählen
   fly volumes create wortlet_data --size 1 --region fra
   ```
3. In der erzeugten `fly.toml` ergänzen bzw. prüfen:
   ```toml
   [http_service]
     internal_port = 8080
     force_https = true

   [mounts]
     source = "wortlet_data"
     destination = "/data"
   ```
4. Geheimnisse setzen (landen verschlüsselt bei Fly, nicht im Repository):
   ```bash
   fly secrets set OpenRouter__ApiKey=sk-or-v1-… Jwt__Key="$(openssl rand -base64 48)" OpenRouter__Referer=https://<deine-app>.fly.dev
   ```
5. `fly deploy` – fertig: `https://<deine-app>.fly.dev`.
   Mit SQLite nur **eine** Maschine laufen lassen: `fly scale count 1`.
6. Sicherungen ins Volume legen (sonst gehen sie beim nächsten Deploy verloren): `fly secrets set Backup__Directory=/data/backups`.

---

## Aufbau

```
backend/
  src/
    Wortlet.Domain/          Fachmodell (DDD): Aggregate Language, Folder (Kapitel, Wörter, Fehlerwörter),
                             RecognitionUsage; Domain-Services AnswerEvaluator, VocabularyText, Grading
    Wortlet.Application/     Anwendungsfälle nach Feature: Auth, Languages, Folders, Quiz, Backup,
                             Recognition, Onboarding – mit DTOs, Validierung und Schnittstellen
    Wortlet.Infrastructure/  EF Core (SQLite/PostgreSQL, Migrationen), Repositories, Identity + JWT,
                             OpenRouter-Client, Startdaten (starter-content.json)
    Wortlet.Api/             Controller nach Feature (Features/*), Fehlerbehandlung, Program.cs
  tests/
    Wortlet.UnitTests/       Antwortvergleich, Textformat, Fehlerwort-Regeln, Import/Export,
                             OpenRouter-Antworten und Ausweichmodelle, Startdaten
    Wortlet.IntegrationTests/ Echte API mit SQLite und simuliertem OpenRouter
frontend/
  src/
    app/                     Routing, Grundgerüst
    features/                auth, languages, folders, quiz, weak-words, recognition, backup
    shared/                  API-Client, UI-Bausteine, Styles
  e2e/                       Playwright-End-to-End-Test
e2e/                         Simuliertes OpenRouter für den End-to-End-Test
```

Die Prüfungslogik (Vergleich, Rückwärts-Antworten, Unterschiede) stammt 1:1 aus der ursprünglichen HTML-Version: Ob eine Antwort stimmt, entscheidet der Server; die Markierung der Unterschiede rechnet das Frontend.

## Tests

```bash
cd backend && dotnet test                  # Unit- und Integrationstests
cd frontend && npm test                    # Vergleichs-, Diff- und Parser-Logik
cd frontend && npx playwright install chromium && E2E_BASE_URL=http://localhost:8080 npm run e2e   # gegen die laufende App
```

Auf GitHub laufen bei jedem Push alle Tests sowie ein End-to-End-Test mit Docker (Registrieren, Prüfung, Topf, Fehlerliste, „War doch richtig“, Richtung umkehren, eigener Ordner, Neuladen, Export/Import, Bilderkennung mit simuliertem OpenRouter). Die Screenshots liegen danach im Artefakt `e2e-ergebnisse`.
