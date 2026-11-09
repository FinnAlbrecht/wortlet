import { useState, type FormEvent } from "react";
import { useNavigate, useParams } from "react-router";
import { errorMessage } from "../../shared/api/client";
import { ConfirmBox } from "../../shared/ui/ConfirmBox";
import { PageHeader } from "../../shared/ui/PageHeader";
import { useAsync } from "../../shared/ui/useAsync";
import { languagesApi } from "../languages/languagesApi";
import { Avatar } from "../profile/Avatar";
import { setsApi } from "../sets/setsApi";
import { groupsApi, VISIBILITY, type GroupDetail, type GroupShare } from "./groupsApi";
import { SHARED_INFO, VisibilityPicker } from "./GroupsPage";

const formatCode = (code: string) => `${code.slice(0, 4)}-${code.slice(4)}`;
const dayFmt = new Intl.DateTimeFormat("de-CH", { day: "numeric", month: "numeric" });

/** Eigene Ordner und Sets, die die Leitung teilen kann. */
function ShareForm({ group, onShared }: { group: GroupDetail; onShared: (g: GroupDetail) => void }) {
  const { data: options, loading } = useAsync(async () => {
    const languages = await languagesApi.list();
    return Promise.all(languages.map(async (l) => ({ language: l, folders: await languagesApi.folders(l.id), sets: await setsApi.list(l.id) })));
  }, []);
  const [choice, setChoice] = useState("");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState("");

  async function share(e: FormEvent) {
    e.preventDefault();
    const [kind, id] = choice.split(":");
    setBusy(true);
    setError("");
    try {
      onShared(await groupsApi.share(group.id, kind === "set" ? { setId: id } : { folderId: id }));
      setChoice("");
    } catch (err) {
      setError(errorMessage(err));
    } finally {
      setBusy(false);
    }
  }

  if (loading || !options) return <p className="loading">Wird geladen …</p>;
  return (
    <form className="stack" onSubmit={share}>
      <label className="field" htmlFor="sharechoice">
        Ordner oder Set teilen
        <select id="sharechoice" value={choice} onChange={(e) => setChoice(e.target.value)}>
          <option value="">– auswählen –</option>
          {options.map(({ language, folders, sets }) => (
            <optgroup key={language.id} label={language.name}>
              {folders.map((f) => <option key={f.id} value={`folder:${f.id}`}>Ordner: {f.name} ({f.wordCount})</option>)}
              {sets.map((s) => <option key={s.id} value={`set:${s.id}`}>Set: {s.name} ({s.wordCount})</option>)}
            </optgroup>
          ))}
        </select>
      </label>
      {error && <p className="error" role="alert">{error}</p>}
      <div className="row">
        <button type="submit" className="primary" id="sharebtn" disabled={busy || !choice}>Mit der Gruppe teilen</button>
      </div>
    </form>
  );
}

export function GroupPage() {
  const { groupId = "" } = useParams();
  const navigate = useNavigate();
  const { data: group, setData, error, loading } = useAsync(() => groupsApi.get(groupId), [groupId]);
  const [confirm, setConfirm] = useState<null | { text: string; label: string; action: () => Promise<void> }>(null);
  const [renaming, setRenaming] = useState(false);
  const [newName, setNewName] = useState("");
  const [busy, setBusy] = useState(false);
  const [message, setMessage] = useState<{ ok: boolean; text: string } | null>(null);

  if (loading && !group) return <p className="loading">Wird geladen …</p>;
  if (error || !group)
    return (
      <>
        <div className="row"><button type="button" className="ghost small" onClick={() => navigate("/groups")}>← Alle Gruppen</button></div>
        <p className="error" role="alert">{error || "Diese Gruppe gibt es nicht."}</p>
      </>
    );

  async function run(action: () => Promise<void>) {
    setBusy(true);
    setMessage(null);
    try {
      await action();
    } catch (e) {
      setMessage({ ok: false, text: errorMessage(e) });
    } finally {
      setBusy(false);
      setConfirm(null);
    }
  }

  const g = group;
  const update = (next: GroupDetail) => setData(next);

  async function copyShare(share: GroupShare) {
    const r = await groupsApi.copy(g.id, share.id);
    update(await groupsApi.get(g.id));
    setMessage({
      ok: true,
      text: r.updated ? `„${share.name}“ ist jetzt auf dem neusten Stand.` : `„${share.name}“ ist jetzt in deiner Sprache ${share.languageName}.`,
    });
  }

  async function copyCode() {
    try {
      await navigator.clipboard.writeText(formatCode(g.joinCode));
      setMessage({ ok: true, text: "Code kopiert." });
    } catch {
      setMessage({ ok: false, text: "Kopieren ging nicht – schreibe den Code ab." });
    }
  }

  async function rename(e: FormEvent) {
    e.preventDefault();
    await run(async () => {
      update(await groupsApi.rename(g.id, newName));
      setRenaming(false);
    });
  }

  return (
    <>
      <PageHeader title={g.name} subtitle={`${VISIBILITY[g.visibility].label} · ${g.ranking.length} ${g.ranking.length === 1 ? "Mitglied" : "Mitglieder"}${g.isLeader ? " · du leitest diese Gruppe" : ""}`} />
      <div className="row">
        <button type="button" className="ghost small" onClick={() => navigate("/groups")}>← Alle Gruppen</button>
        {g.isLeader ? (
          <>
            <button type="button" className="ghost small" id="renamegroup" onClick={() => { setRenaming(true); setNewName(g.name); }}>Umbenennen</button>
            <button type="button" className="ghost small danger" id="delgroup" onClick={() => setConfirm({
              text: `Gruppe „${g.name}“ löschen? Die Kopien, die Mitglieder übernommen haben, bleiben bei ihnen.`,
              label: "Ja, löschen",
              action: async () => { await groupsApi.remove(g.id); navigate("/groups"); },
            })}>Gruppe löschen</button>
          </>
        ) : (
          <button type="button" className="ghost small danger" id="leavegroup" onClick={() => setConfirm({
            text: `Gruppe „${g.name}“ verlassen? Übernommene Ordner bleiben bei dir.`,
            label: "Ja, verlassen",
            action: async () => { await groupsApi.leave(g.id); navigate("/groups"); },
          })}>Gruppe verlassen</button>
        )}
      </div>

      {renaming && (
        <form className="card" onSubmit={(e) => void rename(e)}>
          <label className="field" htmlFor="grouprename">
            Neuer Name
            <input type="text" id="grouprename" autoFocus value={newName} maxLength={60} onChange={(e) => setNewName(e.target.value)} />
          </label>
          <div className="row">
            <button type="submit" className="primary" disabled={busy || !newName.trim()}>Speichern</button>
            <button type="button" className="ghost" onClick={() => setRenaming(false)}>Abbrechen</button>
          </div>
        </form>
      )}
      {confirm && <ConfirmBox text={confirm.text} confirmLabel={confirm.label} busy={busy} onConfirm={() => void run(confirm.action)} onCancel={() => setConfirm(null)} />}
      {message && <p className={message.ok ? "success" : "error"} role={message.ok ? "status" : "alert"}>{message.text}</p>}

      {g.isLeader && g.joinRequests.length > 0 && (
        <section className="card weakcard" id="joinrequests">
          <div className="label">Beitrittsanfragen</div>
          <ol className="ranking">
            {g.joinRequests.map((r) => (
              <li key={r.profile.userId}>
                <Avatar profile={r.profile} size={40} />
                <span className="fbody">
                  <span className="rname">{r.profile.displayName}</span>
                  <span className="fmeta">möchte beitreten</span>
                </span>
                <button type="button" className="primary small" disabled={busy}
                  onClick={() => void run(async () => update(await groupsApi.acceptRequest(g.id, r.profile.userId)))}>Annehmen</button>
                <button type="button" className="ghost small danger" disabled={busy} aria-label={`Anfrage von ${r.profile.displayName} ablehnen`}
                  onClick={() => void run(async () => update(await groupsApi.declineRequest(g.id, r.profile.userId)))}>Ablehnen</button>
              </li>
            ))}
          </ol>
        </section>
      )}

      <section className="card" id="ranking">
        <div className="label">Rangliste diese Woche</div>
        <p className="muted">Geübte Wörter seit Montag, {dayFmt.format(new Date(g.weekStart + "T12:00:00"))}</p>
        <ol className="ranking">
          {g.ranking.map((r, k) => (
            <li key={r.profile.userId} className={r.isMe ? "me" : undefined}>
              <span className="rank">{k + 1}.</span>
              <Avatar profile={r.profile} size={40} />
              <span className="fbody">
                <span className="rname">{r.profile.displayName}{r.isMe && " (du)"}{r.isLeader && <span className="badge">Leitung</span>}</span>
                <span className="fmeta">{r.currentStreak > 0 ? `🔥 ${r.currentStreak} ${r.currentStreak === 1 ? "Tag" : "Tage"} in Folge` : "Keine Lernserie"}</span>
              </span>
              <span className="rscore">{r.weekAnswers}</span>
              {g.isLeader && !r.isMe && (
                <button type="button" className="ghost small danger" aria-label={`${r.profile.displayName} entfernen`} onClick={() => setConfirm({
                  text: `${r.profile.displayName} aus der Gruppe entfernen?`,
                  label: "Ja, entfernen",
                  action: async () => update(await groupsApi.removeMember(g.id, r.profile.userId)),
                })}>✕</button>
              )}
            </li>
          ))}
        </ol>
      </section>

      <section className="card" id="shares">
        <div className="label">Geteilt in dieser Gruppe</div>
        {g.shares.length === 0 && (
          <p className="muted">{g.isLeader ? "Noch nichts geteilt. Teile unten einen Ordner oder ein Set – alle Mitglieder können es dann als Kopie übernehmen." : "Die Leitung hat noch nichts geteilt."}</p>
        )}
        <div className="folderlist">
          {g.shares.map((s) => (
            <div key={s.id} className="share">
              <span className={s.kind === "set" ? "seticon" : "ficon"} aria-hidden="true">{s.kind === "set" ? "★" : ""}</span>
              <span className="fbody">
                <span className="fname">{s.name}</span>
                <span className="fmeta">{s.kind === "set" ? "Set" : "Ordner"} · {s.languageName} · {s.wordCount} Wörter</span>
                <span className="row shareactions">
                  {s.isMine ? (
                    <span className="fmeta">Von dir geteilt</span>
                  ) : s.myCopyFolderId ? (
                    <>
                      <button type="button" className="primary small" onClick={() => navigate(`/folders/${s.myCopyFolderId}`)}>Öffnen</button>
                      <button type="button" className="ghost small" disabled={busy} onClick={() => void run(() => copyShare(s))}>Aktualisieren</button>
                    </>
                  ) : (
                    <button type="button" className="primary small" disabled={busy} onClick={() => void run(() => copyShare(s))}>Übernehmen</button>
                  )}
                  {g.isLeader && (
                    <button type="button" className="ghost small danger" disabled={busy} onClick={() => void run(async () => update(await groupsApi.unshare(g.id, s.id)))}>Nicht mehr teilen</button>
                  )}
                </span>
              </span>
            </div>
          ))}
        </div>
        {g.shares.length > 0 && (
          <p className="muted">„Übernehmen“ legt eine eigene Kopie an – mit eigener Fehlerliste. „Aktualisieren“ holt Änderungen der Leitung.</p>
        )}
        {g.isLeader && <ShareForm group={g} onShared={update} />}
      </section>

      {g.isLeader && (
        <section className="card" id="visibilitycard">
          <div className="label">Wer darf beitreten?</div>
          <VisibilityPicker
            name="groupvisibility"
            value={g.visibility}
            onChange={(v) => {
              if (v === g.visibility || busy) return;
              void run(async () => {
                update(await groupsApi.setVisibility(g.id, v));
                setMessage({
                  ok: true,
                  text: v === "public" && g.joinRequests.length > 0
                    ? "Die Gruppe ist jetzt öffentlich. Offene Anfragen wurden angenommen."
                    : `Die Gruppe ist jetzt „${VISIBILITY[v].label}“.`,
                });
              });
            }}
          />
        </section>
      )}

      <section className="card" id="invite">
        <div className="label">Andere einladen</div>
        <p className="joincode" id="joincodeview">{formatCode(g.joinCode)}</p>
        <p className="muted">Wer diesen Code unter „Gruppen“ eingibt, tritt bei. {SHARED_INFO}</p>
        <div className="row">
          <button type="button" className="ghost small" onClick={() => void copyCode()}>Code kopieren</button>
          {g.isLeader && (
            <button type="button" className="ghost small" disabled={busy} onClick={() => setConfirm({
              text: "Neuen Code erzeugen? Der alte Code funktioniert danach nicht mehr (wer schon drin ist, bleibt).",
              label: "Ja, neuer Code",
              action: async () => update(await groupsApi.newCode(g.id)),
            })}>Neuer Code</button>
          )}
        </div>
      </section>
    </>
  );
}
