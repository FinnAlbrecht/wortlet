import { useLayoutEffect, useRef, useState, type RefObject } from "react";
import { insertAt, upper } from "./logic/specialChars";

interface Props {
  chars: string[];
  inputRef: RefObject<HTMLInputElement | null>;
  value: string;
  onChange: (value: string) => void;
  disabled?: boolean;
}

/** Leiste mit Sonderzeichen unter dem Antwortfeld. Ein Klick fügt das Zeichen an der Cursor-Position ein. */
export function SpecialCharBar({ chars, inputRef, value, onChange, disabled }: Props) {
  const [caps, setCaps] = useState(false);
  const caret = useRef<number | null>(null);

  // Sobald der neue Text im Feld steht: Cursor hinter das eingefügte Zeichen setzen.
  useLayoutEffect(() => {
    const el = inputRef.current;
    if (caret.current === null || !el) return;
    el.focus();
    el.setSelectionRange(caret.current, caret.current);
    caret.current = null;
  }, [value, inputRef]);

  if (chars.length === 0) return null;

  function put(ch: string) {
    const el = inputRef.current;
    const r = insertAt(value, el?.selectionStart ?? null, el?.selectionEnd ?? null, caps ? upper(ch) : ch);
    caret.current = r.caret;
    onChange(r.value);
    setCaps(false); // wie die Shift-Taste: gilt für ein Zeichen
  }

  return (
    <div className="charbar" id="charbar" role="group" aria-label="Sonderzeichen einfügen">
      <button
        type="button"
        className={"charkey shift" + (caps ? " on" : "")}
        aria-pressed={caps}
        aria-label="Grossbuchstabe"
        title="Nächstes Zeichen gross"
        disabled={disabled}
        onMouseDown={(e) => e.preventDefault()}
        onClick={() => setCaps((c) => !c)}
      >
        ⇧
      </button>
      {chars.map((ch) => {
        const shown = caps ? upper(ch) : ch;
        return (
          <button
            type="button"
            key={ch}
            className="charkey"
            aria-label={`${shown} einfügen`}
            disabled={disabled}
            // Fokus bleibt im Antwortfeld, die Tastatur auf dem Handy geht nicht zu.
            onMouseDown={(e) => e.preventDefault()}
            onClick={() => put(ch)}
          >
            {shown}
          </button>
        );
      })}
    </div>
  );
}
