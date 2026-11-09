import { useState } from "react";
import { errorMessage } from "../../shared/api/client";
import { ConfirmBox } from "../../shared/ui/ConfirmBox";
import { backupApi, type BackupDocument } from "./backupApi";

/** „Daten exportieren“ (JSON) und „Daten importieren“ mit Rückfrage vor dem Überschreiben. */
export function BackupCard({ onImported }: { onImported: () => void }) {
  const [message, setMessage] = useState("");
  const [error, setError] = useState("");
  const [pending, setPending] = useState<BackupDocument | null>(null);
  const [busy, setBusy] = useState(false);

  async function doExport() {
    setError("");
    setMessage("");
    try {
      const doc = await backupApi.export();
      const blob = new Blob([JSON.stringify(doc, null, 2)], { type: "application/json" });
      const url = URL.createObjectURL(blob);
      const a = document.createElement("a");
      a.href = url;
      a.download = `vokabeltrainer-${new Date().toISOString().slice(0, 10)}.json`;
      document.body.appendChild(a);
      a.click();
      a.remove();
      setTimeout(() => URL.revokeObjectURL(url), 1000);
      const folders = doc.languages.reduce((n, l) => n + l.folders.length, 0);
      setMessage(`Exportiert: ${doc.languages.length} Sprachen, ${folders} Ordner.`);
    } catch (e) {
      setError(errorMessage(e));
    }
  }

  async function chooseFile(file: File | undefined) {
    setError("");
    setMessage("");
    if (!file) return;
    try {
      const doc = JSON.parse(await file.text()) as BackupDocument;
      if (!doc || !Array.isArray(doc.languages)) throw new Error();
      setPending(doc);
    } catch {
      setError("Diese Datei ist keine Sicherung des Vokabeltrainers.");
    }
  }

  async function doImport() {
    if (!pending) return;
    setBusy(true);
    try {
      const r = await backupApi.import(pending);
      setMessage(`Importiert: ${r.languages} Sprachen, ${r.folders} Ordner, ${r.words} Wörter, ${r.weakWords} Fehlerwörter.`);
      setPending(null);
      onImported();
    } catch (e) {
      setError(errorMessage(e));
      setPending(null);
    } finally {
      setBusy(false);
    }
  }

  const folderCount = pending?.languages.reduce((n, l) => n + (l.folders?.length ?? 0), 0) ?? 0;

  return (
    <section className="card" id="backup">
      <div className="label">Daten sichern</div>
      <p className="muted">Lade alle Sprachen, Ordner und Fehlerwörter als Datei herunter oder stelle eine Sicherung wieder her.</p>
      <div className="row">
        <button type="button" className="ghost small" id="export" onClick={() => void doExport()}>Daten exportieren</button>
        <label className="filebtn" htmlFor="importfile" id="importbtn" style={{ padding: "6px 12px", fontSize: ".85rem" }}>
          Daten importieren
          <input
            type="file"
            id="importfile"
            accept="application/json,.json"
            onChange={(e) => {
              void chooseFile(e.target.files?.[0]);
              e.target.value = "";
            }}
          />
        </label>
      </div>
      {pending && (
        <ConfirmBox
          id="importconfirm"
          text={`Achtung: Alle deine jetzigen Sprachen, Ordner und Fehlerwörter werden durch die Datei ersetzt (${pending.languages.length} Sprachen, ${folderCount} Ordner). Fortfahren?`}
          confirmLabel="Ja, überschreiben"
          busy={busy}
          onConfirm={() => void doImport()}
          onCancel={() => setPending(null)}
        />
      )}
      {message && <p className="success" id="backupstatus" aria-live="polite">{message}</p>}
      {error && <p className="error" role="alert">{error}</p>}
    </section>
  );
}
