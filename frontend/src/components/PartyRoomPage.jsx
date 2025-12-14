import { useNavigate, useParams } from "react-router-dom";
import { useCallback, useEffect, useRef, useState } from "react";
import "./PartyRoomPage.css";
import * as signalR from "@microsoft/signalr";

const API_URL = (import.meta.env.VITE_API_URL ?? "https://api.cotunes.online");
const PLACEHOLDER_VIDEO = "cX9BSDR2vZ4";

// Extract YouTube ID from many common formats
function extractYouTubeId(input) {
  if (!input) return null;
  const trimmed = input.trim();
  // Try common URL forms first
  const urlRegex = /(?:https?:\/\/)?(?:www\.)?(?:youtube\.com\/(?:watch\?.*v=|embed\/)|youtu\.be\/)([a-zA-Z0-9_-]{11})/;
  const match = trimmed.match(urlRegex);
  if (match) return match[1];
  // Fallback if raw ID
  const idRegex = /^[a-zA-Z0-9_-]{11}$/;
  return idRegex.test(trimmed) ? trimmed : null;
}

export default function PartyRoomPage() {
  const { id: roomId } = useParams();
  const navigate = useNavigate();

  // ROOM STATE
  const [room, setRoom] = useState({
    members: [],
    queue: [],
    name: "",
    capacity: 0,
    isPrivate: false,
  });

  // AUTH
  const [isAuthenticated, setIsAuthenticated] = useState(false);
  const [username, setUsername] = useState("");

  // SIGNALR & PLAYER
  const [connection, setConnection] = useState(null);
  const connectionRef = useRef(null);
  const playerRef = useRef(null);
  const playerReadyRef = useRef(false);

  // UI state
  const [playerReady, setPlayerReady] = useState(false); // used to drive UI
  const [bufferedEvents, setBufferedEvents] = useState([]);
  const pendingVideoRef = useRef(null);
  const isReloadRef = useRef(false);
  const lastEndedRef = useRef(0);

  //Scoll state
  const chatRef = useRef(null);
  const shouldAutoScrollRef = useRef(true);

  const [messages, setMessages] = useState([]);
  const [newMessage, setNewMessage] = useState("");
  const [isMuted, setIsMuted] = useState(true);

  // 1. AUTH CHECK
  useEffect(() => {
    let mounted = true;
    const checkAuth = async () => {
      try {
        const res = await fetch(`${API_URL}/Account/Me`, { credentials: "include" });
        if (!res.ok) {
          navigate("/login");
        } else {
          const data = await res.json();
          if (!mounted) return;
          setIsAuthenticated(true);
          setUsername(data.userName);
        }
      } catch (err) {
        console.error("Auth check failed:", err);
        navigate("/login");
      }
    };
    checkAuth();
    return () => {
      mounted = false;
    };
  }, [navigate]);

  // 2. FETCH ROOM INFO
  useEffect(() => {
    let abort = false;
    fetch(`${API_URL}/${roomId}`)
      .then((res) => res.json())
      .then((data) => {
        if (!abort) setRoom(data);
      })
      .catch((err) => console.error("Error fetching room:", err));
    return () => {
      abort = true;
    };
  }, [roomId]);

  // 3. Helper: load a video safely (cue then play to avoid race/stutter)
  const loadVideoSafely = useCallback((videoId, startSeconds = 0, autoPlay = true) => {
    const p = playerRef.current;
    if (!p) {
      // If player not ready, remember pending
      pendingVideoRef.current = { videoId, seek: startSeconds, autoPlay };
      return;
    }

    const currentId = p.getVideoData()?.video_id;
    try {
      if (currentId !== videoId) {
        // Cue (preloads) and rely on onStateChange(CUED) to kick off play
        pendingVideoRef.current = { videoId, seek: startSeconds, autoPlay };
        // use cueVideoById to avoid trying to play while still buffering in an unstable state
        if (typeof p.cueVideoById === "function") {
          p.cueVideoById(videoId, startSeconds);
          setTimeout(() => {
            try {
              p.playVideo();
            } catch { }
          }, 200); // 200ms delay -- without this video does not autoplay after skip
        } else {
          // fallback
          p.loadVideoById(videoId, startSeconds);
          setTimeout(() => {
            try {
              p.playVideo();
            } catch { }
          }, 200); // 200ms delay -- without this video does not autoplay after skip
        }
      } else {
        // Same video: seek and play/pause as requested
        const delta = Math.abs(p.getCurrentTime() - startSeconds);
        if (delta > 2) p.seekTo(startSeconds, true);
        if (autoPlay) p.playVideo();
        else p.pauseVideo();
        pendingVideoRef.current = null;
      }
    } catch (err) {
      // keep pending if an error occurs
      pendingVideoRef.current = { videoId, seek: startSeconds, autoPlay };
      console.error("loadVideoSafely failed:", err);
    }
  }, []);

  // Flush buffered events when player becomes ready
  const flushEvents = useCallback(() => {
    const p = playerRef.current;
    if (!playerReadyRef.current || !p) return;

    const pending = pendingVideoRef.current;
    if (pending?.videoId) {
      loadVideoSafely(pending.videoId, pending.seek ?? 0, pending.autoPlay ?? true);
    }

    // Use current bufferedEvents snapshot then clear
    setBufferedEvents((events) => {
      if (!events || events.length === 0) return [];
      events.forEach((event) => {
        switch (event.type) {
          case "load":
            loadVideoSafely(event.videoId, event.time ?? 0, true);
            break;
          case "seek":
            try {
              if (p && typeof p.seekTo === "function") p.seekTo(event.time, true);
            } catch {}
            break;
          case "play":
            try {
              if (p && typeof p.playVideo === "function") p.playVideo();
            } catch {}
            break;
          case "pause":
            try {
              if (p && typeof p.pauseVideo === "function") p.pauseVideo();
            } catch {}
            break;
          default:
            break;
        }
      });
      return [];
    });
  }, [loadVideoSafely]);

  useEffect(() => {
    flushEvents();
  }, [playerReady, flushEvents]);

  // 4. SETUP SIGNALR CONNECTION
  useEffect(() => {
    if (!isAuthenticated) return;
    const conn = new signalR.HubConnectionBuilder()
      .withUrl(`${API_URL}/hubs/partyroom`)
      .withAutomaticReconnect([0, 2000, 5000, 10000])
      .build();

    connectionRef.current = conn;
    isReloadRef.current = false;

    const bufferOrPlay = (event) => {
      if (!playerReadyRef.current || !playerRef.current) {
        setBufferedEvents((prev) => [...prev, event]);
      } else {
        const p = playerRef.current;
        switch (event.type) {
          case "load":
            loadVideoSafely(event.videoId, event.time ?? 0, true);
            break;
          case "seek":
            p.seekTo(event.time, true);
            break;
          case "play":
            p.playVideo();
            break;
          case "pause":
            p.pauseVideo();
            break;
          default:
            break;
        }
      }
    };

    const handleUnload = () => {
      const nav = performance.getEntriesByType("navigation")[0];
      if (nav && nav.type === "reload") {
        isReloadRef.current = true;
        return;
      }
      navigator.sendBeacon(`${API_URL}/${roomId}/leave`);
    };
    window.addEventListener("beforeunload", handleUnload);

    conn.on("SyncTime", ({ videoId, time, isPlaying }) => {
      if (!playerReadyRef.current || !playerRef.current) {
        pendingVideoRef.current = { videoId, seek: time, autoPlay: isPlaying };
        return;
      }
      const p = playerRef.current;
      if (videoId !== p.getVideoData()?.video_id) {
        loadVideoSafely(videoId, time ?? 0, isPlaying);
      } else {
        const delta = Math.abs(p.getCurrentTime() - (time ?? 0));
        if (delta > 2) p.seekTo(time ?? 0, true);
        if (isPlaying) p.playVideo();
        else p.pauseVideo();
      }
    });

    conn.on("MemberListUpdated", (members) => setRoom((prev) => ({ ...prev, members })));
    conn.on("QueueUpdated", (_, queue) => setRoom((prev) => ({ ...prev, queue })));


    // Chat & votes
    conn.on("ReceiveMessage", (user, message) => setMessages((prev) => [...prev, { user, message }]));
    conn.on("VoteRequested", (action) =>
      setMessages((prev) => [...prev, { system: true, message: `Vote started: ${action}?`, action }])
    );
    conn.on("VoteResult", (action, passed) =>
      setMessages((prev) => [...prev, { system: true, message: `Vote result for ${action}: ${passed ? "✅ Passed" : "❌ Failed"}` }])
    );

    conn.on("LoadVideo", (videoId) => bufferOrPlay({ type: "load", videoId }));
    conn.on("SeekTo", (time) => bufferOrPlay({ type: "seek", time }));
    conn.on("Play", () => bufferOrPlay({ type: "play" }));
    conn.on("Pause", () => bufferOrPlay({ type: "pause" }));

    const syncInterval = setInterval(() => {
      if (
        connectionRef.current &&
        playerRef.current &&
        playerReadyRef.current &&
        playerRef.current.getPlayerState() === 1 // playing
      ) {
        try {
          const currentTime = playerRef.current.getCurrentTime();
          connectionRef.current.invoke("UpdateTime", roomId, currentTime).catch(() => { });
        } catch {
          // ignore
        }
      }
    }, 3000);

    conn
      .start()
      .then(async () => {
        console.log("Connected to SignalR hub");
        // Wait for player to become ready (if not already)
        if (!playerReadyRef.current) {
          await new Promise((resolve) => {
            const check = setInterval(() => {
              if (playerReadyRef.current) {
                clearInterval(check);
                resolve();
              }
            }, 100);
          });
        }
        await conn.invoke("JoinRoom", parseInt(roomId));
      })
      .then(() => {
        setConnection(conn);
      })
      .catch((err) => console.error("SignalR connection failed:", err));

    conn.onreconnected(() => {
      console.log("Reconnected — rejoining room...");
      conn.invoke("JoinRoom", parseInt(roomId)).catch(() => { });
    });

    return () => {
      clearInterval(syncInterval);
      window.removeEventListener("beforeunload", handleUnload);
      if (!isReloadRef.current) conn.invoke("LeaveRoom", parseInt(roomId)).catch(() => { });
      conn.stop().catch(() => { });
    };
  }, [roomId, isAuthenticated, loadVideoSafely]);

  // 5. LOAD YOUTUBE IFRAME API & INIT PLAYER
  useEffect(() => {
    if (!room) return;

    let scriptAdded = false;
    const initPlayer = () => {
      // If a player already exists, do not reinit
      if (playerRef.current) return;

      const firstVideoId = room.queue?.[0]?.TrackId || PLACEHOLDER_VIDEO;
      const ytPlayer = new window.YT.Player("player", {
        height: "360",
        width: "640",
        videoId: firstVideoId,
        playerVars: { autoplay: 1, controls: 1, origin: window.location.origin, mute: 1, playsinline: 1},
        events: {
          onReady: (e) => {
            playerRef.current = e.target;
            setPlayerReady(true);
            playerReadyRef.current = true;
            // Keep initial mute state consistent
            if (isMuted) e.target.mute();
            else e.target.unMute();
            // If there was a pending video set before player was ready, cue it now
            if (pendingVideoRef.current?.videoId) {
              const pvd = pendingVideoRef.current;
              loadVideoSafely(pvd.videoId, pvd.seek ?? 0, pvd.autoPlay ?? true);
            } else {
              e.target.playVideo();
            }
          },
          onStateChange: (event) => {
            const p = playerRef.current;
            if (!p || !connectionRef.current) return;
            const time = p.getCurrentTime();

            // If we transition to CUED (5), and we have a pendingVideo flagged for autoplay, start playback.
            if (typeof window !== "undefined" && window.YT && event.data === window.YT.PlayerState.CUED) {
              const pending = pendingVideoRef.current;
              if (pending?.autoPlay) {
                try {
                  // If start position provided, seek just to be safe
                  if (pending.seek != null) {
                    p.seekTo(pending.seek, true);
                  }
                  p.playVideo();
                } catch {}
                // clear pending
                pendingVideoRef.current = null;
              }
            }

            // YouTube states: 1 = playing, 2 = paused, 0 = ended
            if (event.data === window.YT.PlayerState.PLAYING) {
              connectionRef.current.invoke("Play", roomId, time).catch(() => { });
            } else if (event.data === window.YT.PlayerState.PAUSED) {
              connectionRef.current.invoke("Pause", roomId, time).catch(() => { });
            } else if (event.data === window.YT.PlayerState.ENDED) {
              // Debounce multiple ENDED events
              const now = Date.now();
              if (now - lastEndedRef.current < 2000) return;
              lastEndedRef.current = now;

              // Ask server to advance the queue and start the next track
              try {
                if (connectionRef.current && connectionRef.current.state === signalR.HubConnectionState.Connected) {
                  connectionRef.current.invoke("SkipTrack", parseInt(roomId)).catch(() => { });
                }
              } catch {}
            }
          },
        },
      });
    };

    if (window.YT && window.YT.Player) {
      initPlayer();
    } else {
      // Prevent double-inserting the script
      if (!document.getElementById("youtube-iframe-api")) {
        const tag = document.createElement("script");
        tag.id = "youtube-iframe-api";
        tag.src = "https://www.youtube.com/iframe_api";
        scriptAdded = true;
        window.onYouTubeIframeAPIReady = initPlayer;
        document.body.appendChild(tag);
      } else {
        // If script exists but API not ready yet, ensure global ready hook is set
        window.onYouTubeIframeAPIReady = initPlayer;
      }
    }

    return () => {
      if (scriptAdded && document.getElementById("youtube-iframe-api")) {
      }
      try {
        if (window.onYouTubeIframeAPIReady && window.onYouTubeIframeAPIReady === initPlayer) {
          window.onYouTubeIframeAPIReady = undefined;
        }
      } catch {
        // ignore
      }
    };
  }, [room, roomId, isMuted, loadVideoSafely]);

  // HANDLERS
  const handleChatScroll = () => {
    const el = chatRef.current;
    if (!el) return;

    const threshold = 50; // px from bottom
    const atBottom =
      el.scrollHeight - el.scrollTop - el.clientHeight < threshold;

    shouldAutoScrollRef.current = atBottom;
  };

  const handleLeaveRoom = useCallback(async () => {
    try {
      if (connectionRef.current && connectionRef.current.state === signalR.HubConnectionState.Connected) {
        await connectionRef.current.invoke("LeaveRoom", parseInt(roomId));
      }
      await fetch(`${API_URL}/${roomId}/leave`, { method: "POST" });
      navigate("/partyrooms");
      window.location.reload();
    } catch (err) {
      console.error("Failed to leave room:", err);
      alert("Failed to leave room.");
    }
  }, [navigate, roomId]);

  const handleSendMessage = useCallback(async () => {
    if (!newMessage.trim()) return;
    try {
      if (!connection || connection.state !== signalR.HubConnectionState.Connected) {
        alert("Not connected to server. Please wait and try again.");
        return;
      }
      await connection.invoke("SendMessage", roomId.toString(), username, newMessage);
      setNewMessage("");
    } catch (err) {
      console.error("SendMessage failed:", err);
      alert("Failed to send message.");
    }
  }, [connection, newMessage, roomId, username]);

  useEffect(() => {
    const el = chatRef.current;
    if (!el) return;

    if (shouldAutoScrollRef.current) {
      el.scrollTo({
        top: el.scrollHeight,
        behavior: "smooth",
      });
    }
  }, [messages]);

  // UI
  if (!room) return <p>Loading room...</p>;

  return (
    <div className="partyroom-page">
      <div className="partyroom-header">
        <h2>{room.name}</h2>
        <div>
          Members: {room.members?.length || 0} / {room.capacity}
          <ul>
            {room.members?.map((m, i) => (
              <li key={i}>{m}</li>
            ))}
          </ul>
          <button onClick={handleLeaveRoom}>Leave Room</button>
        </div>
      </div>

      <div className="partyroom-main">
        <div className="queue-panel">
          <h3>Queue</h3>
          <div className="queue-list">
            {room.queue && room.queue.length > 0 ? (
              <ul>
                {room.queue.map((track, i) => {
                  const isPlaceholder = track.trackId === "placeholder" || !track.trackId;
                  const title = isPlaceholder
                    ? "No video in queue"
                    : track.title || track.trackId || "Unknown";

                  return (
                    <li key={i}>
                      {title}
                    </li>
                  );
                })}
              </ul>
            ) : (
              <p>(No tracks yet)</p>
            )}
          </div>
        </div>

        <div className="partyroom-content">
          <p className="partyroom-info">{room.isPrivate ? "Private" : "Public"} room</p>

          <div id="player-wrapper">
            <div id="player"></div>
            <div className="player-overlay"></div>
          </div>

          <div className="partyroom-buttons">
            <button
              onClick={async () => {
                const input = prompt("Enter YouTube link or ID:");
                if (!input) return;

                const videoId = extractYouTubeId(input);
                if (!videoId) {
                  alert("Invalid YouTube link or ID");
                  return;
                }

                if (!connection || connection.state !== signalR.HubConnectionState.Connected) {
                  alert("Not connected to server yet. Try again in a moment.");
                  return;
                }

                try {
                  // Call the backend method that handles first-track replacement and normal enqueue
                  await connection.invoke("EnqueueTrack", parseInt(roomId), videoId);
                  console.log("Track added successfully");
                } catch (err) {
                  console.error("Failed to add track:", err);
                  alert("Failed to add track.");
                }
              }}
            >
              Add to Queue
            </button>

            <button
              onClick={() => {
                if (!connection || connection.state !== signalR.HubConnectionState.Connected) return;
                connection.invoke("RequestVote", roomId.toString(), "Skip").catch(() => { });
              }}
            >
              Vote Skip
            </button>

            <button
              onClick={() => {
                if (!connection || connection.state !== signalR.HubConnectionState.Connected) return;
                connection.invoke("RequestVote", roomId.toString(), "Play").catch(() => { });
              }}
            >
              Vote Play
            </button>
            <button
              onClick={() => {
                if (!connection || connection.state !== signalR.HubConnectionState.Connected) return;
                connection.invoke("RequestVote", roomId.toString(), "Pause").catch(() => { });
              }}
            >
              Vote Pause
            </button>
          </div>
        </div>

        <div className="chat-panel">
          <div
            className="chat-messages"
            ref={chatRef}
            onScroll={handleChatScroll}
          >
            {messages.map((m, i) => (
              <div key={i}>
                {m.system ? (
                  <>
                    <strong>System:</strong> {m.message}
                    {m.action && (
                      <span>
                        {" "}
                        <button
                          onClick={() =>
                            connection?.invoke("CastVote", roomId.toString(), username, true).catch(() => { })
                          }
                        >
                          👍
                        </button>
                        <button
                          onClick={() =>
                            connection?.invoke("CastVote", roomId.toString(), username, false).catch(() => { })
                          }
                        >
                          👎
                        </button>
                      </span>
                    )}
                  </>
                ) : (
                  <>
                    <strong>{m.user}:</strong> {m.message}
                  </>
                )}
              </div>
            ))}
          </div>

          <div className="chat-input">
            <input
              value={newMessage}
              onChange={(e) => setNewMessage(e.target.value)}
              onKeyDown={(e) => {
                if (e.key === "Enter") {
                  e.preventDefault();
                  handleSendMessage();
                }
              }}
              placeholder="Type a message..."
            />
            <button onClick={handleSendMessage}>Send</button>
          </div>

          <div className="volume-control">
            <label>Volume:</label>
            <input
              type="range"
              min="0"
              max="100"
              defaultValue="50"
              onChange={(e) => {
                const p = playerRef.current;
                if (p && playerReadyRef.current) p.setVolume(parseInt(e.target.value, 10));
              }}
            />

            <button
              onClick={() => {
                const p = playerRef.current;
                if (!p) return;
                if (isMuted) {
                  p.unMute();
                  setIsMuted(false);
                } else {
                  p.mute();
                  setIsMuted(true);
                }
              }}
            >
              {isMuted ? "Unmute" : "Mute"}
            </button>
          </div>
        </div>
      </div>
    </div>
  );
}