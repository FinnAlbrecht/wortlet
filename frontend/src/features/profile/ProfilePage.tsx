import { useEffect, useState, type ChangeEvent, type FormEvent } from "react";
import { errorMessage } from "../../shared/api/client";
import { PageHeader } from "../../shared/ui/PageHeader";
import { useAsync } from "../../shared/ui/useAsync";
import { Avatar } from "./Avatar";
import { profileApi } from "./profileApi";
import { useProfile } from "./ProfileContext";

/** Schneidet die Mitte quadratisch aus und verkleinert auf 256 px (JPEG, ca. 20–40 KB). */
async function squareAvatar(file: File, side = 256): Promise<Blob> {
  const bmp = await createImageBitmap(file);
  const s = Math.min(bmp.width, bmp.height);
  const canvas = document.createElement("canvas");
  canvas.width = side;
  canvas.height = side;
  const ctx = canvas.getContext("2d");
  if (!ctx) throw new Error("Das Bild konnte nicht verarbeitet werden.");
  ctx.drawImage(bmp, (bmp.width - s) / 2, (bmp.height - s) / 2, s, s, 0, 0, side, side);
  bmp.close?.();
  const blob = await new Promise<Blob | null>((resolve) => canvas.toBlob(resolve, "image/jpeg", 0.85));
  if (!blob) throw new Error("Das Bild konnte nicht verarbeitet werden.");
  return blob;
}

export function ProfilePage() {
  const { profile, setProfile } = useProfile();
  const { data: options } = useAsync(() => profileApi.options(), []);
  const [name, setName] = useState("");
  const [icon, setIcon] = useState("");
  const [color, setColor] = useState("");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState("");
  const [saved, setSaved] = useState("");

  useEffect(() => {
    if (!profile) return;
    setName(profile.displayName);
    setIcon(profile.avatarIcon);
    setColor(profile.avatarColor);
  }, [profile]);

  if (!profile || !options) return <p className="loading">Wird geladen …</p>;

  async function run(action: () => Promise<void>, success: string) {
    setBusy(true);
    setError("");
    setSaved("");
    try {
      await action();
      setSaved(success);
    } catch (e) {
      setError(errorMessage(e));
    } finally {
      setBusy(false);
    }
  }

  function save(e: FormEvent) {
    e.preventDefault();
    void run(async () => setProfile(await profileApi.update({ displayName: name, avatarIcon: icon, avatarColor: color })), "Gespeichert.");
  }

  function upload(e: ChangeEvent<HTMLInputElement>) {
    const file = e.target.files?.[0];
    e.target.value = "";
    if (!file) return;
    void run(async () => setProfile(await profileApi.uploadImage(await squareAvatar(file))), "Bild gespeichert.");
  }

  const preview = { ...profile, displayName: name || profile.displayName, avatarIcon: icon, avatarColor: color };

  return (
    <>
      <PageHeader title="Profil" subtitle={profile.email} />
      <form className="card" id="profileform" onSubmit={save}>
        <div className="profilehead">
          <Avatar profile={preview} size={72} />
          <div className="fbody">
            <span className="fname">{preview.displayName}</span>
            <span className="fmeta">So sehen dich andere in deinen Gruppen.</span>
          </div>
        </div>

        <label className="field" htmlFor="displayname">
          Name
          <input type="text" id="displayname" value={name} maxLength={40} autoComplete="nickname" onChange={(e) => setName(e.target.value)} />
        </label>

        <div className="label">Symbol</div>
        <div className="picker" role="radiogroup" aria-label="Symbol">
          {options.icons.map((i) => (
            <label key={i} className="pick">
              <input type="radio" name="icon" value={i} checked={icon === i} onChange={() => setIcon(i)} />
              <span style={{ background: color }}>{i}</span>
            </label>
          ))}
        </div>

        <div className="label">Farbe</div>
        <div className="picker" role="radiogroup" aria-label="Farbe">
          {options.colors.map((c) => (
            <label key={c} className="pick">
              <input type="radio" name="color" value={c} checked={color === c} onChange={() => setColor(c)} aria-label={`Farbe ${c}`} />
              <span style={{ background: c }} />
            </label>
          ))}
        </div>

        {error && <p className="error" role="alert">{error}</p>}
        {saved && <p className="success" role="status">{saved}</p>}
        <div className="row">
          <button type="submit" className="primary" id="profilesave" disabled={busy || !name.trim()}>Speichern</button>
        </div>
      </form>

      <section className="card" id="avatarcard">
        <div className="label">Eigenes Bild</div>
        <p className="muted">
          Statt eines Symbols kannst du ein Foto verwenden. Es wird quadratisch zugeschnitten und verkleinert.
          Sehen können es nur Mitglieder deiner Gruppen.
        </p>
        <div className="row">
          <label className={"filebtn" + (busy ? " disabled" : "")}>
            {profile.hasImage ? "Anderes Bild wählen" : "Bild wählen"}
            <input type="file" id="avatarfile" accept="image/jpeg,image/png,image/webp" disabled={busy} onChange={upload} />
          </label>
          {profile.hasImage && (
            <button type="button" className="ghost danger" id="avatarremove" disabled={busy}
              onClick={() => void run(async () => setProfile(await profileApi.removeImage()), "Bild entfernt.")}>
              Bild entfernen
            </button>
          )}
        </div>
      </section>
    </>
  );
}
