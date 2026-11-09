import { useEffect, useRef } from "react";

/** Rote Rückfrage wie im Original („Ja, löschen“ / „Nein“). */
export function ConfirmBox(props: {
  id?: string;
  text: string;
  confirmLabel?: string;
  busy?: boolean;
  onConfirm: () => void;
  onCancel: () => void;
}) {
  const noRef = useRef<HTMLButtonElement>(null);
  useEffect(() => noRef.current?.focus(), []);
  return (
    <div className="fb bad" id={props.id} role="alertdialog" aria-label={props.text}>
      <span>{props.text}</span>
      <div className="row" style={{ marginTop: 10 }}>
        <button type="button" className="small dangerfill" onClick={props.onConfirm} disabled={props.busy}>
          {props.confirmLabel ?? "Ja, löschen"}
        </button>
        <button type="button" className="ghost small" ref={noRef} onClick={props.onCancel} disabled={props.busy}>
          Nein
        </button>
      </div>
    </div>
  );
}
