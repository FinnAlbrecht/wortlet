import { useEffect, useState, type FormEvent } from "react";
import { useNavigate } from "react-router";
import { errorMessage } from "../../shared/api/client";
import { PageHeader } from "../../shared/ui/PageHeader";
import { useAsync } from "../../shared/ui/useAsync";
import { groupsApi, VISIBILITY, type GroupSearchResult, type Visibility } from "./groupsApi";

export const SHARED_INFO =
  "Die anderen Mitglieder sehen deinen Namen, dein Profilbild, wie viele Wörter du diese Woche geübt hast und deine Lernserie – nicht deine E-Mail-Adresse und nicht deine Ordner.";

/** Wahl zwischen Privat / Auf Anfrage / Öffentlich. */
export function VisibilityPicker({ value, onChange, name }: { value: Visibility; onChange: (v: Visibility) => void; name: string }) {
  return (
    <div className="visibility" role="radiogroup" aria-label="Sichtbarkeit">
      {(Object.keys(VISIBILITY) as Visibility[]).map((v) => (
        <label key={v} className={value === v ? "on" : undefined}>
          <input type="radio" name={name} value={v} checked={value === v} onChange={() => onChange(v)} />
          <span className="vlabel">{VISIBILITY[v].label}</span>
          <span className="fmeta">{VISIBILITY[v].text}</span>
        </label>
      ))}
    </div>
  );
}

/** Gruppen nach Namen suchen (nur öffentliche und solche „auf Anfrage“). */
function GroupSearch({ onChanged }: { onChanged: () => void }) {
  const navigate = useNavigate();
  const [query, setQuery] = useState("");
  const [results, setResults] = useState<GroupSearchResult[] | null>(null);
  const [error, setError] = useState("");
  const [busy, setBusy] = useState("");
  const q = query.trim();

  // Kurz warten, bis fertig getippt ist.
  useEffect(() => {
    setError("");
    if (q.length < 2) {
      setResults(null);
      return;
    }
    const ctrl = new AbortController();
    const timer = setTimeout(() => {
      groupsApi
        .search(q, ctrl.signal)
        .then(setResults)
        .catch((e) => {
          if (!(e instanceof DOMException && e.name === "AbortError")) setError(errorMessage(e));
        });
    }, 300);
    return () => {
      clearTimeout(timer);
      ctrl.abort();
    };
  }, [q]);

  async function act(g: GroupSearchResult) {
    setBusy(g.id);
    setError("");
    try {
      if (g.status === "member") return navigate(`/groups/${g.id}`);
      if (g.status === "requested") {
        await groupsApi.cancelRequest(g.id);
        setResults((r) => r?.map((x) => (x.id === g.id ? { ...x, status: "none" } : x)) ?? null);
      } else {
        const r = await groupsApi.joinFromSearch(g.id);
        if (r.status === "joined") return navigate(`/groups/${g.id}`);
        setResults((list) => list?.map((x) => (x.id === g.id ? { ...x, status: "requested" } : x)) ?? null);
      }
      onChanged();
    } catch (e) {
      setError(errorMessage(e));
    } finally {
      setBusy("");
    }
  }

  const label = (g: GroupSearchResult) =>
    g.status === "member" ? "Öffnen" : g.status === "requested" ? "Anfrage zurückziehen" : g.visibility === "public" ? "Beitreten" : "Anfrage senden";

  return (
    <section className="card" id="groupsearch">
      <div className="label">Gruppe suchen</div>
      <label className="field" htmlFor="groupquery">
        Name der Gruppe
        <input type="text" id="groupquery" placeholder="z. B. Klasse 3b" value={query} autoComplete="off" onChange={(e) => setQuery(e.target.value)} />
      </label>
      <p className="muted">Gefunden werden nur öffentliche Gruppen und Gruppen „auf Anfrage“. Private Gruppen erreichst du nur mit dem Code.</p>
      {error && <p className="error" role="alert">{error}</p>}
      {results && results.length === 0 && <p className="muted">Keine Gruppe gefunden.</p>}
      {results && results.length > 0 && (
        <div className="folderlist" id="searchresults">
          {results.map((g) => (
            <div key={g.id} className="share">
              <span className="lcode" aria-hidden="true">{g.name.slice(0, 2).toUpperCase()}</span>
              <span className="fbody">
                <span className="fname">{g.name}</span>
                <span className="fmeta">
                  {VISIBILITY[g.visibility].label} · {g.memberCount} {g.memberCount === 1 ? "Mitglied" : "Mitglieder"} · Leitung: {g.leaderName}
                </span>
                {g.status === "requested" && <span className="potchip fchip">Anfrage gesendet – wartet auf die Leitung</span>}
                <span className="row shareactions">
                  <button type="button" className={g.status === "requested" ? "ghost small" : "primary small"} disabled={!!busy} onClick={() => void act(g)}>
                    {busy === g.id ? "…" : label(g)}
                  </button>
                </span>
              </span>
            </div>
          ))}
        </div>
      )}
      {results && results.some((g) => g.status === "none") && <p className="muted">{SHARED_INFO}</p>}
    </section>
  );
}

export function GroupsPage() {
  const navigate = useNavigate();
  const { data, error, loading, reload } = useAsync(async () => {
    const [groups, requests] = await Promise.all([groupsApi.list(), groupsApi.myRequests()]);
    return { groups, requests };
  }, []);
  const [code, setCode] = useState("");
  const [name, setName] = useState("");
  const [visibility, setVisibility] = useState<Visibility>("private");
  const [busy, setBusy] = useState<"" | "join" | "create">("");
  const [formError, setFormError] = useState<{ form: "join" | "create"; text: string } | null>(null);

  async function submit(kind: "join" | "create", e: FormEvent) {
    e.preventDefault();
    setBusy(kind);
    setFormError(null);
    try {
      const group = kind === "join" ? await groupsApi.join(code) : await groupsApi.create(name, visibility);
      navigate(`/groups/${group.id}`);
    } catch (err) {
      setFormError({ form: kind, text: errorMessage(err) });
      setBusy("");
    }
  }

  async function cancel(groupId: string) {
    try {
      await groupsApi.cancelRequest(groupId);
    } finally {
      void reload();
    }
  }

  const groups = data?.groups;
  const requests = data?.requests ?? [];

  return (
    <>
      <PageHeader title="Gruppen" subtitle="Zusammen mit der Klasse oder Freunden üben" />
      <section className="card" id="grouplistcard">
        <div className="label">Deine Gruppen</div>
        {loading && !groups && <p className="loading">Wird geladen …</p>}
        {error && <p className="error" role="alert">{error} <button type="button" className="ghost small" onClick={reload}>Nochmals</button></p>}
        {groups?.length === 0 && <p className="muted">Du bist noch in keiner Gruppe. Suche eine, tritt mit einem Code bei oder erstelle eine eigene.</p>}
        <div className="folderlist" id="grouplist">
          {groups?.map((g) => (
            <button type="button" key={g.id} className="folder" id={`group-${g.id}`} onClick={() => navigate(`/groups/${g.id}`)}>
              <span className="lcode" aria-hidden="true">{g.name.slice(0, 2).toUpperCase()}</span>
              <span className="fbody">
                <span className="fname">{g.name}</span>
                <span className="fmeta">
                  {VISIBILITY[g.visibility].label} · {g.memberCount} {g.memberCount === 1 ? "Mitglied" : "Mitglieder"} · {g.shareCount} geteilt{g.isLeader ? " · du leitest" : ""}
                </span>
                {g.pendingRequests > 0 && (
                  <span className="potchip fchip">{g.pendingRequests} {g.pendingRequests === 1 ? "neue Anfrage" : "neue Anfragen"}</span>
                )}
              </span>
            </button>
          ))}
        </div>
        {requests.length > 0 && (
          <>
            <div className="label">Deine offenen Anfragen</div>
            <div className="list" id="myrequests">
              {requests.map((r) => (
                <div key={r.groupId}>
                  <span>{r.name}</span>
                  <span className="reqaction">
                    <button type="button" className="ghost small" onClick={() => void cancel(r.groupId)}>Zurückziehen</button>
                  </span>
                </div>
              ))}
            </div>
          </>
        )}
      </section>

      <GroupSearch onChanged={reload} />

      <form className="card" id="joinform" onSubmit={(e) => void submit("join", e)}>
        <div className="label">Mit Code beitreten</div>
        <label className="field" htmlFor="joincode">
          Code
          <input type="text" id="joincode" className="code" placeholder="z. B. K7M2-QX9P" value={code} maxLength={20} autoComplete="off" autoCapitalize="characters" spellCheck={false} onChange={(e) => setCode(e.target.value)} />
        </label>
        <p className="muted">Mit dem Code trittst du jeder Gruppe direkt bei – auch privaten. {SHARED_INFO}</p>
        {formError?.form === "join" && <p className="error" role="alert">{formError.text}</p>}
        <div className="row">
          <button type="submit" className="primary" id="joinbtn" disabled={!!busy || code.trim().length < 8}>{busy === "join" ? "Trete bei …" : "Beitreten"}</button>
        </div>
      </form>

      <form className="card" id="createform" onSubmit={(e) => void submit("create", e)}>
        <div className="label">Neue Gruppe erstellen</div>
        <label className="field" htmlFor="groupname">
          Name
          <input type="text" id="groupname" placeholder="z. B. Klasse 3b" value={name} maxLength={60} onChange={(e) => setName(e.target.value)} />
        </label>
        <div className="label">Wer darf beitreten?</div>
        <VisibilityPicker value={visibility} onChange={setVisibility} name="newvisibility" />
        <p className="muted">Du leitest die Gruppe: Du bekommst einen Code zum Weitergeben und kannst Ordner und Sets mit allen teilen. Die Einstellung kannst du später ändern.</p>
        {formError?.form === "create" && <p className="error" role="alert">{formError.text}</p>}
        <div className="row">
          <button type="submit" className="primary" id="createbtn" disabled={!!busy || !name.trim()}>{busy === "create" ? "Erstelle …" : "Erstellen"}</button>
        </div>
      </form>
    </>
  );
}
