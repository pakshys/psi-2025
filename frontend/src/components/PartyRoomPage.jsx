import { useNavigate, useParams } from "react-router-dom";
import { useState, useEffect, useRef } from "react";
import "./PartyRoomPage.css";
import * as signalR from "@microsoft/signalr";

const API_URL = "https://localhost:7234/partyroom";

export default function PartyRoomPage() {
  const { id: roomId } = useParams();
  const navigate = useNavigate();

  // === ROOM STATE ===
  const [room, setRoom] = useState({
    members: [],
    queue: [],
    name: "",
    capacity: 0,
    isPrivate: false,
  });

  // === AUTH STATE ===
  const [isAuthenticated, setIsAuthenticated] = useState(false);
  const [username, setUsername] = useState("");

  // === SIGNALR & PLAYER STATE ===
  const [connection, setConnection] = useState(null);
  const connectionRef = useRef(null); // prevent stale closure
  const [player, setPlayer] = useState(null);
  const playerRef = useRef(null);
  const [playerReady, setPlayerReady] = useState(false);
  const playerReadyRef = useRef(false);
  const [bufferedEvents, setBufferedEvents] = useState([]);
  const [pendingVideo, setPendingVideo] = useState(null);
  const [isMuted, setIsMuted] = useState(true);
  const PLACEHOLDER_VIDEO = "dQw4w9WgXcQ";

  // === CHAT STATE ===
  const [messages, setMessages] = useState([]);
  const [newMessage, setNewMessage] = useState("");

  // === 1. AUTH CHECK ===
  useEffect(() => {
    const checkAuth = async () => {
      try {
        const res = await fetch("https://localhost:7234/Account/Me", { credentials: "include" });
        if (!res.ok) {
          navigate("/login");
        } else {
          const data = await res.json();
          setIsAuthenticated(true);
          setUsername(data.userName);
        }
      } catch (err) {
        console.error("Auth check failed:", err);
        navigate("/login");
      }
    };
    checkAuth();
  }, [navigate]);

  // === 2. FETCH ROOM INFO ===
  useEffect(() => {
    fetch(`${API_URL}/${roomId}`)
      .then((res) => res.json())
      .then((data) => setRoom(data))
      .catch((err) => console.error("Error fetching room:", err));
  }, [roomId]);

  // === 3. BUFFERED EVENTS HANDLER ===
  const flushEvents = () => {
    if (!playerReadyRef.current || !playerRef.current) return;

    // process pending video first
    if (pendingVideo?.videoId) {
      playerRef.current.loadVideoById(pendingVideo.videoId);
      if (pendingVideo.seek != null) playerRef.current.seekTo(pendingVideo.seek, true);
      setPendingVideo(null);
    }

    // process buffered events
    bufferedEvents.forEach((event) => {
      const p = playerRef.current;
      switch (event.type) {
        case "load": p.loadVideoById(event.videoId); break;
        case "seek": p.seekTo(event.time, true); break;
        case "play": p.playVideo(); break;
        case "pause": p.pauseVideo(); break;
      }
    });

    setBufferedEvents([]);
  };

  useEffect(() => flushEvents(), [playerReady, player, bufferedEvents, pendingVideo]);

  // === 4. SETUP SIGNALR CONNECTION ===
  useEffect(() => {
    if (!isAuthenticated) return;

    const conn = new signalR.HubConnectionBuilder()
      .withUrl("https://localhost:7234/hubs/partyroom")
      .withAutomaticReconnect([0, 2000, 5000, 10000])
      .build();

    connectionRef.current = conn;

    let isReload = false;

    // --- Helper: buffer events if player not ready ---
    const bufferOrPlay = (event) => {
      if (!playerReadyRef.current || !playerRef.current) {
        setBufferedEvents((prev) => [...prev, event]);
      } else {
        const p = playerRef.current;
        switch (event.type) {
          case "load": p.loadVideoById(event.videoId); break;
          case "seek": p.seekTo(event.time, true); break;
          case "play": p.playVideo(); break;
          case "pause": p.pauseVideo(); break;
        }
      }
    };

    // --- Handler for page reload / leave ---
    const handleUnload = () => {
      const nav = performance.getEntriesByType("navigation")[0];
      if (nav && nav.type === "reload") {
        isReload = true;
        return;
      }
      navigator.sendBeacon(`${API_URL}/${roomId}/leave`);
    };
    window.addEventListener("beforeunload", handleUnload);

    // --- Setup SignalR event handlers ---
    conn.on("SyncTime", (time) => {
      if (!playerReadyRef.current || !playerRef.current) {
        setBufferedEvents((prev) => [...prev, { type: "seek", time }]);
        return;
      }
      const currentTime = playerRef.current.getCurrentTime();
      if (Math.abs(currentTime - time) > 2) playerRef.current.seekTo(time, true);
    });

    conn.on("MemberListUpdated", (members) => setRoom((prev) => ({ ...prev, members })));
    conn.on("QueueUpdated", (_, queue) => setRoom((prev) => ({ ...prev, queue })));
    conn.on("ReceiveMessage", (user, message) => setMessages((prev) => [...prev, { user, message }]));
    conn.on("VoteRequested", (action) => setMessages((prev) => [...prev, { system: true, message: `Vote started: ${action}?`, action }]));
    conn.on("VoteResult", (action, passed) => setMessages((prev) => [...prev, { system: true, message: `Vote result for ${action}: ${passed ? "✅ Passed" : "❌ Failed"}` }]));

    conn.on("LoadVideo", (videoId) => bufferOrPlay({ type: "load", videoId }));
    conn.on("SeekTo", (time) => bufferOrPlay({ type: "seek", time }));
    conn.on("Play", () => bufferOrPlay({ type: "play" }));
    conn.on("Pause", () => bufferOrPlay({ type: "pause" }));

    // --- Periodic sync ---
    const syncInterval = setInterval(() => {
      if (
        connectionRef.current &&
        playerRef.current &&
        playerReadyRef.current &&
        playerRef.current.getPlayerState() === 1
      ) {
        const currentTime = playerRef.current.getCurrentTime();
        connectionRef.current.invoke("UpdateTime", roomId, currentTime).catch(() => { });
      }
    }, 3000);

    // --- Start connection ---
    conn.start()
      .then(async () => {
        console.log("Connected to SignalR hub");

        // Wait until YouTube player ready
        await new Promise((resolve) => {
          if (playerReadyRef.current) resolve();
          else {
            const check = setInterval(() => {
              if (playerReadyRef.current) { clearInterval(check); resolve(); }
            }, 100);
          }
        });

        await conn.invoke("JoinRoom", parseInt(roomId));
      })
      .then(() => setConnection(conn))
      .catch((err) => console.error("SignalR connection failed:", err));

    conn.onreconnected(() => {
      console.log("Reconnected — rejoining room...");
      conn.invoke("JoinRoom", parseInt(roomId)).catch(() => { });
    });

    // --- Cleanup on unmount ---
    return () => {
      clearInterval(syncInterval);
      window.removeEventListener("beforeunload", handleUnload);
      if (!isReload) conn.invoke("LeaveRoom", parseInt(roomId)).catch(() => { });
      conn.stop().catch(() => { });
    };
  }, [roomId, isAuthenticated, playerReady, player]);

  // === 5. LOAD YOUTUBE IFRAME API ===
  useEffect(() => {
    if (!room) return;

    function initPlayer() {
      const ytPlayer = new window.YT.Player("player", {
        height: "360",
        width: "640",
        videoId: room.queue?.[0]?.TrackId || PLACEHOLDER_VIDEO,
        playerVars: { autoplay: 1, controls: 1, origin: window.location.origin, mute: 1 },
        events: {
          onReady: (e) => {
            setPlayer(e.target);
            playerRef.current = e.target;
            setPlayerReady(true);
            playerReadyRef.current = true;
            e.target.playVideo(); // start muted autoplay

            // broadcast local play/pause
            e.target.addEventListener("onStateChange", (event) => {
              if (!connectionRef.current) return;
              const time = e.target.getCurrentTime();
              switch (event.data) {
                case 1: connectionRef.current.invoke("Play", roomId, time).catch(() => { }); break;
                case 2: connectionRef.current.invoke("Pause", roomId, time).catch(() => { }); break;
              }
            });
          },
        },
      });
    }

    if (window.YT && window.YT.Player) initPlayer();
    else {
      const tag = document.createElement("script");
      tag.src = "https://www.youtube.com/iframe_api";
      window.onYouTubeIframeAPIReady = initPlayer;
      document.body.appendChild(tag);
    }
  }, [room, roomId]);

  // === 6. HANDLE PENDING VIDEO ===
  useEffect(() => flushEvents(), [playerReady, player, pendingVideo]);

  // === 7. HANDLERS ===
  const handleLeaveRoom = () => {
    fetch(`${API_URL}/${roomId}/leave`, { method: "POST" })
      .then(() => navigate("/partyrooms"))
      .catch((err) => alert(err.message));
  };

  const handleSendMessage = () => {
    if (newMessage.trim() && connectionRef.current) {
      connectionRef.current.invoke("SendMessage", roomId, username, newMessage);
      setNewMessage("");
    }
  };


  // === 6. Render UI ===
  if (!room) return <p>Loading room...</p>;

  //Layout
  return (
    <div className="partyroom-page">
      {/* === Top bar === */}
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

      {/* === Main content row === */}
      <div className="partyroom-main">
        {/* === Queue (Left) === */}
        <div className="queue-panel">
          <h3>Queue</h3>
          <div className="queue-list">
            {room.queue && room.queue.length > 0 ? (
              <ul>
                {room.queue.map((track, i) => {
                  const isPlaceholder = track.TrackId === "placeholder" || !track.TrackId;
                  const title = isPlaceholder
                    ? "No video loaded"
                    : track.Title || track.title || track.TrackId || "Unknown";
                  const creator = isPlaceholder
                    ? ""
                    : track.Creator || track.creator || track.ChannelTitle || "Unknown";

                  return (
                    <li key={i}>
                      {title} {creator && `— ${creator}`}
                    </li>
                  );
                })}
              </ul>
            ) : (
              <p>(No tracks yet)</p>
            )}
          </div>
        </div>

        {/* === Video & Controls (Center) === */}
        <div className="partyroom-content">
          <p className="partyroom-info">
            {room.isPrivate ? "Private" : "Public"} room
          </p>

          <div id="player-wrapper">
            <div id="player"></div>
            <div className="player-overlay"></div>
          </div>

          <div className="partyroom-buttons">
            <button
              onClick={async () => {
                let input = prompt("Enter YouTube link or ID:");
                if (!input) return;

                const match = input.match(/(?:v=|\/)([a-zA-Z0-9_-]{11})(?:[?&]|$)/);
                const videoId = match ? match[1] : input.trim();
                if (!videoId) return;

                if (!connection || connection.state !== "Connected") {
                  alert("Not connected to server yet. Try again in a moment.");
                  return;
                }

                try {
                  await connection.invoke("EnqueueTrack", parseInt(roomId), videoId);
                } catch (err) {
                  console.error("Enqueue/Load failed:", err);
                  alert("Failed to enqueue or load the track.");
                }
              }}
            >
              Add to Queue
            </button>

            <button
              onClick={() => {
                if (!connection || connection.state !== "Connected") return;
                connection.invoke("RequestVote", roomId.toString(), "Skip");
              }}
            >
              Vote Skip
            </button>

            <button onClick={() => connection.invoke("RequestVote", roomId.toString(), "Play")}>Vote Play</button>
            <button onClick={() => connection.invoke("RequestVote", roomId.toString(), "Pause")}>Vote Pause</button>
          </div>
        </div>

        {/* === Chat (Right) === */}
        <div className="chat-panel">
          <div className="chat-messages">
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
                            connection.invoke(
                              "CastVote",
                              roomId.toString(),
                              username,
                              true
                            )
                          }
                        >
                          👍
                        </button>
                        <button
                          onClick={() =>
                            connection.invoke(
                              "CastVote",
                              roomId.toString(),
                              username,
                              false
                            )
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

          {/* === Volume Slider === */}
          <div className="volume-control">
            <label>Volume:</label>
            <input
              type="range"
              min="0"
              max="100"
              defaultValue="50"
              onChange={(e) => {
                if (player && playerReady) player.setVolume(parseInt(e.target.value));
              }}
            />

            {player && playerReady && (
              <button
                onClick={() => {
                  if (!player) return;
                  if (isMuted) {
                    player.unMute();
                    setIsMuted(false);
                  } else {
                    player.mute();
                    setIsMuted(true);
                  }
                }}
              >
                {isMuted ? "Unmute" : "Mute"}
              </button>
            )}
          </div>
        </div>
      </div>
    </div>
  );
}
