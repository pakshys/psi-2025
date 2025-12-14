import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import "./Friends.css";

const API_URL = "https://localhost:7234/Friendship";

async function readBody(res) {
    if (res.status === 204) return null;
    const ct = res.headers.get("content-type") || "";
    if (ct.includes("application/json")) return await res.json();
    return null;
}

async function getJson(path) {
    const res = await fetch(`${API_URL}${path}`, {
        credentials: "include",
        headers: { Accept: "application/json" },
    });

    const body = await readBody(res);

    if (!res.ok) {
        const msg = body?.error || body?.message || `${res.status} ${res.statusText}`;
        throw new Error(msg);
    }

    return body;
}

async function sendJson(method, path) {
    const res = await fetch(`${API_URL}${path}`, {
        method,
        credentials: "include",
        headers: { Accept: "application/json" },
    });

    const body = await readBody(res);

    if (!res.ok) {
        const msg = body?.error || body?.message || `${res.status} ${res.statusText}`;
        throw new Error(msg);
    }

    return body;
}

export default function Friends() {
    const navigate = useNavigate();

    const [accepted, setAccepted] = useState([]);
    const [pending, setPending] = useState([]); // incoming
    const [outgoing, setOutgoing] = useState([]); // sent

    const [newUsername, setNewUsername] = useState("");
    const [loading, setLoading] = useState(false);
    const [err, setErr] = useState("");
    const [okMsg, setOkMsg] = useState("");

    async function refresh() {
        setErr("");
        setOkMsg("");
        setLoading(true);

        try {
            const [a, p, o] = await Promise.all([
                getJson("/list"),                 // accepted only
                getJson("/pending"),              // incoming pending
                getJson("/outgoing").catch(() => []) // outgoing pending (jei endpointo nėra - tuščias)
            ]);

            setAccepted(Array.isArray(a) ? a : []);
            setPending(Array.isArray(p) ? p : []);
            setOutgoing(Array.isArray(o) ? o : []);
        } catch (e) {
            setErr(String(e.message || e));
        } finally {
            setLoading(false);
        }
    }

    useEffect(() => {
        refresh();
    }, []);

    async function onAdd() {
        const username = newUsername.trim();
        if (!username) return;

        setErr("");
        setOkMsg("");
        setLoading(true);

        try {
            await sendJson("POST", `/add/by-username/${encodeURIComponent(username)}`);
            setNewUsername("");
            setOkMsg("Friend request sent.");
            await refresh();
        } catch (e) {
            setErr(String(e.message || e));
            setLoading(false);
        }
    }

    async function onAccept(id) {
        setErr("");
        setOkMsg("");
        setLoading(true);

        try {
            await sendJson("POST", `/accept/${id}`);
            setOkMsg("Friend request accepted.");
            await refresh();
        } catch (e) {
            setErr(String(e.message || e));
            setLoading(false);
        }
    }

    async function onReject(id) {
        setErr("");
        setOkMsg("");
        setLoading(true);

        try {
            await sendJson("DELETE", `/reject/${id}`);
            setOkMsg("Friend request rejected.");
            await refresh();
        } catch (e) {
            setErr(String(e.message || e));
            setLoading(false);
        }
    }

    async function onCancel(id) {
        setErr("");
        setOkMsg("");
        setLoading(true);

        try {
            await sendJson("DELETE", `/cancel/${id}`);
            setOkMsg("Friend request cancelled.");
            await refresh();
        } catch (e) {
            setErr(String(e.message || e));
            setLoading(false);
        }
    }

    async function onRemove(id) {
        setErr("");
        setOkMsg("");
        setLoading(true);

        try {
            await sendJson("DELETE", `/remove/${id}`);
            setOkMsg("Friend removed.");
            await refresh();
        } catch (e) {
            setErr(String(e.message || e));
            setLoading(false);
        }
    }

    return (
        <div className="friends-wrap">
            {/* ✅ Return button (identical style to Profile/Login) */}
            <div className="top-right">
                <button
                    className="login-link"
                    onClick={() => navigate("/")}
                    style={{ background: "transparent", cursor: "pointer" }}
                >
                    ← Main menu
                </button>
            </div>

            <h1 className="friends-title">Friends</h1>

            <div className="friends-actions">
                <input
                    className="friends-input"
                    placeholder="Enter friend's username"
                    value={newUsername}
                    onChange={(e) => setNewUsername(e.target.value)}
                    onKeyDown={(e) => {
                        if (e.key === "Enter") onAdd();
                    }}
                />
                <button
                    className="friends-btn"
                    onClick={onAdd}
                    disabled={loading || !newUsername.trim()}
                >
                    {loading ? "..." : "Add"}
                </button>
            </div>

            {err && <div className="friends-error">{err}</div>}
            {okMsg && <div className="friends-empty">{okMsg}</div>}

            <h2 className="friends-subtitle">Pending requests (incoming)</h2>
            {pending.length === 0 ? (
                <div className="friends-empty">There are no pending requests.</div>
            ) : (
                <ul className="friends-list">
                    {pending.map((p) => (
                        <li key={p.friendshipId} className="friends-card">
                            <div className="friends-name">{p.otherUserName}</div>
                            <div className="friends-meta">
                                <span>Status: {p.status}</span>
                                <span>•</span>
                                <span>{new Date(p.createdAt).toLocaleString()}</span>
                            </div>
                            <div className="friends-card-actions">
                                <button
                                    className="friends-btn"
                                    onClick={() => onAccept(p.friendshipId)}
                                    disabled={loading}
                                >
                                    Accept
                                </button>
                                <button
                                    className="friends-btn-ghost"
                                    onClick={() => onReject(p.friendshipId)}
                                    disabled={loading}
                                >
                                    Reject
                                </button>
                            </div>
                        </li>
                    ))}
                </ul>
            )}

            <h2 className="friends-subtitle">Sent requests (outgoing)</h2>
            {outgoing.length === 0 ? (
                <div className="friends-empty">You have no sent requests.</div>
            ) : (
                <ul className="friends-list">
                    {outgoing.map((o) => (
                        <li key={o.friendshipId} className="friends-card">
                            <div className="friends-name">{o.otherUserName}</div>
                            <div className="friends-meta">
                                <span>Status: {o.status}</span>
                                <span>•</span>
                                <span>{new Date(o.createdAt).toLocaleString()}</span>
                            </div>
                            <div className="friends-card-actions">
                                <button
                                    className="friends-btn-ghost"
                                    onClick={() => onCancel(o.friendshipId)}
                                    disabled={loading}
                                >
                                    Cancel
                                </button>
                            </div>
                        </li>
                    ))}
                </ul>
            )}

            <h2 className="friends-subtitle">Friends (accepted)</h2>
            {accepted.length === 0 ? (
                <div className="friends-empty">There are currently 0 friends.</div>
            ) : (
                <ul className="friends-list">
                    {accepted.map((f) => (
                        <li key={f.friendshipId} className="friends-card">
                            <div className="friends-name">{f.otherUserName}</div>
                            <div className="friends-meta">
                                <span>Status: {f.status}</span>
                                <span>•</span>
                                <span>{new Date(f.createdAt).toLocaleString()}</span>
                            </div>
                            <div className="friends-card-actions">
                                <button
                                    className="friends-btn-ghost"
                                    onClick={() => onRemove(f.friendshipId)}
                                    disabled={loading}
                                >
                                    Remove
                                </button>
                            </div>
                        </li>
                    ))}
                </ul>
            )}
        </div>
    );
}

