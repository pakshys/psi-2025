import { useEffect, useState } from "react";
import "./Friends.css";

const API_URL = "https://localhost:7234/Friendship";

async function getJson(path) {
    const res = await fetch(`${API_URL}${path}`, {
        credentials: "include",
        headers: { Accept: "application/json" },
    });
    if (!res.ok) throw new Error(`${res.status} ${res.statusText}`);
    if (res.status === 204) return null;
    return res.json();
}

async function sendJson(method, path) {
    const res = await fetch(`${API_URL}${path}`, {
        method,
        credentials: "include",
        headers: { Accept: "application/json" },
    });
    if (!res.ok) throw new Error(`${res.status} ${res.statusText}`);
    return res.status === 204 ? null : res.json();
}

export default function Friends() {
    const [accepted, setAccepted] = useState([]);
    const [pending, setPending] = useState([]);
    const [newUserId, setNewUserId] = useState("");
    const [loading, setLoading] = useState(false);
    const [err, setErr] = useState("");

    async function refresh() {
        setErr("");
        setLoading(true);
        try {
            const [a, p] = await Promise.all([
                getJson("/list"),
                getJson("/pending"),
            ]);
            setAccepted(Array.isArray(a) ? a : []);
            setPending(Array.isArray(p) ? p : []);
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
        if (!newUserId.trim()) return;
        setErr("");
        setLoading(true);
        try {
            await sendJson("POST", `/add/${encodeURIComponent(newUserId.trim())}`);
            setNewUserId("");
            await refresh();
        } catch (e) {
            setErr(String(e.message || e));
            setLoading(false);
        }
    }

    async function onAccept(id) {
        setErr("");
        setLoading(true);
        try {
            await sendJson("POST", `/accept/${id}`);
            await refresh();
        } catch (e) {
            setErr(String(e.message || e));
            setLoading(false);
        }
    }

    async function onReject(id) {
        setErr("");
        setLoading(true);
        try {
            await sendJson("DELETE", `/reject/${id}`);
            await refresh();
        } catch (e) {
            setErr(String(e.message || e));
            setLoading(false);
        }
    }

    async function onRemove(id) {
        setErr("");
        setLoading(true);
        try {
            await sendJson("DELETE", `/remove/${id}`);
            await refresh();
        } catch (e) {
            setErr(String(e.message || e));
            setLoading(false);
        }
    }

    return (
        <div className="friends-wrap">
            <h1 className="friends-title">Friends</h1>

            <div className="friends-actions">
                <input
                    className="friends-input"
                    placeholder="Enter your friends UserId (GUID)"
                    value={newUserId}
                    onChange={(e) => setNewUserId(e.target.value)}
                />
                <button
                    className="friends-btn"
                    onClick={onAdd}
                    disabled={loading || !newUserId.trim()}
                >
                    {loading ? "..." : "Add"}
                </button>
            </div>

            {err && <div className="friends-error">{err}</div>}

            <h2 className="friends-subtitle">Pending requests</h2>
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
