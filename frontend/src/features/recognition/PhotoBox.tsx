import { useEffect, useState } from "react";
import { ApiError, errorMessage } from "../../shared/api/client";
import { buildChatPrompt, copyToClipboard } from "./copyPrompt";
import { recognitionApi, type RecognitionStatus } from "./recognitionApi";
import { shrinkImage } from "./shrinkImage";

const wait = (ms: number) => new Promise((r) => setTimeout(r, ms));

/** Wörter aus Fotos erkennen (über den Server und OpenRouter) – mit „Prompt kopieren“ als Notlösung. */
export function PhotoBox({ languageName, onLines }: { languageName: string; onLines: (lines: string[]) => void }) {
  const [status, setStatus] = useState<RecognitionStatus | null>(null);
  const [message, setMessage] = useState("");
  const [busy, setBusy] = useState(false);
  const [copied, setCopied] = useState("");

  useEffect(() => {
    recognitionApi.status().then(setStatus).catch(() => setStatus(null));
  }, []);

  async function handleFiles(list: FileList | null) {
    const files = list ? [...list] : [];
    if (!files.length) return;
    setBusy(true);
    let added = 0;
    try {
      for (let k = 0; k < files.length; k++) {
        setMessage(`Lese Foto ${k + 1} von ${files.length} … das kann etwas dauern.`);
        try {
          const img = await shrinkImage(files[k]);
          const result = await recognitionApi.recognize(img, languageName);
          onLines(result.lines);
          added += result.wordCount;
          setStatus((s) => (s ? { ...s, remainingToday: result.remainingToday, usedToday: s.dailyLimit - result.remainingToday } : s));
        } catch (e) {
          const err = e instanceof ApiError ? e : null;
          // Limit erreicht, Modell nicht verfügbar, nicht eingerichtet → aufhören
          if (err && (err.status === 429 || err.status === 503 || err.status === 401 || err.status === 0)) {
            setMessage(errorMessage(e) + (k + 1 < files.length ? ` (Foto ${k + 1} bis ${files.length} wurden nicht gelesen.)` : ""));
            if (err.status === 429) setStatus((s) => (s ? { ...s, remainingToday: 0 } : s));
            return;
          }
          setMessage(`Foto ${k + 1} konnte nicht gelesen werden: ${errorMessage(e)} Es geht mit dem nächsten weiter.`);
          await wait(1500);
        }
      }
      setMessage(`${added} Wörter erkannt. Kontrolliere sie unten, bevor du speicherst.`);
    } finally {
      setBusy(false);
    }
  }

  async function copyPrompt() {
    const ok = await copyToClipboard(buildChatPrompt(languageName));
    setCopied(ok ? "Prompt kopiert. Füge ihn mit dem Foto in einen KI-Chat ein und kopiere die Antwort unten ins Feld." : "Kopieren hat nicht geklappt. Markiere den Text unten und kopiere ihn selbst.");
  }

  const canUpload = status?.configured && status.remainingToday > 0 && !busy;

  return (
    <div className="photo" id="photobox">
      <span className="fieldname">Aus Fotos erkennen</span>
      <p className="muted">
        Fotografiere die Buchseiten. Die KI liest die Wörter heraus und schreibt sie unten ins Feld. Kontrolliere sie danach, bevor du speicherst.
      </p>
      {status?.configured ? (
        <div className="row">
          <label className={"filebtn" + (canUpload ? "" : " disabled")} htmlFor="edphotos">
            {busy ? "Wird gelesen …" : "Fotos auswählen"}
            <input
              type="file"
              id="edphotos"
              accept="image/*"
              multiple
              disabled={!canUpload}
              onChange={(e) => {
                const files = e.target.files;
                void handleFiles(files).finally(() => (e.target.value = ""));
              }}
            />
          </label>
          <span className="helpline" id="photoquota">Heute noch {status.remainingToday} von {status.dailyLimit} Fotos</span>
        </div>
      ) : (
        <p className="helpline" id="photooff">
          {status ? "Die Bilderkennung ist auf diesem Server nicht eingerichtet." : "Bilderkennung wird geprüft …"} Du kannst den Prompt kopieren und ein beliebiges KI-Chatprogramm verwenden.
        </p>
      )}
      <p className="muted" id="photostatus" aria-live="polite">{message}</p>
      <div className="row">
        <button type="button" className="ghost small" id="copyprompt" onClick={() => void copyPrompt()}>Prompt kopieren</button>
      </div>
      {copied && <p className="helpline" id="copystatus" aria-live="polite">{copied}</p>}
    </div>
  );
}
