import { useNavigate, useParams } from "react-router-dom";
import { useState, useEffect } from "react";
import "./PartyRoomPage.css";
import * as signalR from "@microsoft/signalr";

const API_URL = "https://localhost:7234/partyroom";

export default function PartyRoomPage() {
  const { id: roomId } = useParams();
  const [room, setRoom] = useState(null);
  const navigate = useNavigate();
  const [connection, setConnection] = useState(null);
  const [player, setPlayer] = useState(null);
  const [playerReady, setPlayerReady] = useState(false);
  const [pendingVideo, setPendingVideo] = useState(null);
  const [bufferedEvents, setBufferedEvents] = useState([]);
  const [isMuted, setIsMuted] = useState(true); // start muted
  const PLACEHOLDER_VIDEO = "dQw4w9WgXcQ"; // or any neutral video


  useEffect(() => {
    if (playerReady && player && bufferedEvents.length > 0) {
      bufferedEvents.forEach((event) => {
        switch (event.type) {
          case "load": player.loadVideoById(event.videoId); break;
          case "seek": player.seekTo(event.time, true); break;
          case "play": player.playVideo(); break;
          case "pause": player.pauseVideo(); break;
        }
      });
      setBufferedEvents([]);
    }
  }, [playerReady, player]);

  // Chat
  const [messages, setMessages] = useState([]);
  const [newMessage, setNewMessage] = useState("");

  // === 1. Fetch room info ===
  useEffect(() => {
    fetch(`${API_URL}/${roomId}`)
      .then((response) => response.json())
      .then((data) => setRoom(data))
      .catch((error) => console.error("Error fetching room:", error));
  }, [roomId]);

  // === 2. Setup SignalR connection ===
  // === 2. Setup SignalR connection + handle player properly ===
useEffect(() => {
  const conn = new signalR.HubConnectionBuilder()
    .withUrl("https://localhost:7234/hubs/partyroom")
    .withAutomaticReconnect([0, 2000, 5000, 10000])
    .build();

  let isReload = false;

  // --- Handle page reload / leave ---
  const handleUnload = () => {
    const nav = performance.getEntriesByType("navigation")[0];
    if (nav && nav.type === "reload") {
      isReload = true;
      return;
    }
    navigator.sendBeacon(`${API_URL}/${roomId}/leave`);
  };
  window.addEventListener("beforeunload", handleUnload);

  // --- Start connection ---
  conn.start()
    .then(() => {
      console.log("Connected to SignalR hub");

      // --- CLEANUP OLD PLAYER (if exists) ---
      if (player) {
        player.destroy();
        setPlayer(null);
        setPlayerReady(false);
        setBufferedEvents([]);
        setPendingVideo(null);
      }

      // --- JOIN ROOM ---
      return conn.invoke("JoinRoom", parseInt(roomId));
    })
    .then(() => setConnection(conn))
    .catch(err => console.error("SignalR connection failed:", err));

  // --- Reconnect handler ---
  conn.onreconnected(() => {
    console.log("Reconnected — rejoining room...");

    if (player) {
      player.destroy();
      setPlayer(null);
      setPlayerReady(false);
      setBufferedEvents([]);
      setPendingVideo(null);
    }

    conn.invoke("JoinRoom", parseInt(roomId)).catch(() => {});
  });

  // --- CLEANUP on unmount ---
  return () => {
    window.removeEventListener("beforeunload", handleUnload);
    if (!isReload) {
      conn.invoke("LeaveRoom", parseInt(roomId)).catch(() => {});
    }
    conn.stop().catch(() => {});
  };
}, [roomId]);


  // === 3. Load YouTube iframe API ===
  useEffect(() => {
    if (!room) return; // <-- wait until room is fetched

    function initPlayer() {
      const ytPlayer = new window.YT.Player("player", {
        height: "360",
        width: "640",
        videoId: room.queue && room.queue.length > 0 ? room.queue[0].TrackId : PLACEHOLDER_VIDEO,
        playerVars: {
          autoplay: 1,    // enable autoplay
          controls: 1,
          origin: window.location.origin,
          mute: 1,        // mute to allow autoplay
        },
        events: {
          onReady: (e) => {
            setPlayer(e.target);
            setPlayerReady(true);

            // Optional: unmute after first user interaction
            e.target.playVideo();  // start muted autoplay
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
  }, [room]);


  // === 4. Hook SignalR events ===
  useEffect(() => {
    if (!connection) return;

    const safePlayerCall = (callback) => {
      if (playerReady && player) callback();
    };

    connection.on("LoadVideo", (videoId) => {
      console.log("Loading video ID:", videoId);
      if (!videoId || !/^[a-zA-Z0-9_-]{11}$/.test(videoId)) {
        console.warn("Invalid video ID received:", videoId);
        return;
      }
      setPendingVideo({ videoId, seek: 0 }); // keep consistent
      if (!playerReady || !player) {
        setBufferedEvents((prev) => [...prev, { type: "load", videoId }]);
      } else {
        player.loadVideoById(videoId);
      }
    });

    connection.on("SeekTo", (time) => {
      if (!playerReady || !player) {
        setBufferedEvents((prev) => [...prev, { type: "seek", time }]);
      } else {
        player.seekTo(time, true);
      }
    });

    connection.on("Play", () => {
      if (!playerReady || !player) {
        setBufferedEvents((prev) => [...prev, { type: "play" }]);
      } else {
        player.playVideo();
      }
    });

    connection.on("Pause", () => {
      if (!playerReady || !player) {
        setBufferedEvents((prev) => [...prev, { type: "pause" }]);
      } else {
        player.pauseVideo();
      }
    });

    // Room + chat + vote handlers (keep your code here)
    connection.on("PartyRoomUpdated", (updatedRoom) => {
      if (updatedRoom.id === parseInt(roomId)) setRoom(updatedRoom);
    });

    connection.on("ReceiveMessage", (user, message) => {
      setMessages((prev) => [...prev, { user, message }]);
    });

    connection.on("VoteRequested", (action) => {
      setMessages((prev) => [
        ...prev,
        { system: true, message: `Vote started: ${action}?`, action },
      ]);
    });

    connection.on("VoteResult", (action, passed) => {
      setMessages((prev) => [
        ...prev,
        {
          system: true,
          message: `Vote result for ${action}: ${passed ? "✅ Passed" : "❌ Failed"}`,
        },
      ]);
    });

    connection.on("QueueUpdated", (roomId, queue) => {
      console.log("Queue updated:", queue);
      setRoom((prev) => ({ ...prev, queue }));
    });

    // Periodic sync
    const syncInterval = setInterval(() => {
      if (connection && player && player.getCurrentTime && player.getPlayerState() === 1) {
        const currentTime = player.getCurrentTime();
        connection.invoke("UpdateTime", roomId, currentTime).catch(() => {});
      }
    }, 3000);
    
    connection.on("MemberCountUpdated", (count) => {
      setRoom((prev) => ({ ...prev, guestsCount: count }));
    });

    return () => {
      clearInterval(syncInterval);
      connection.off("LoadVideo");
      connection.off("SeekTo");
      connection.off("Play");
      connection.off("Pause");
      connection.off("PartyRoomUpdated");
      connection.off("ReceiveMessage");
      connection.off("VoteRequested");
      connection.off("VoteResult");
      connection.off("QueueUpdated");
    };
  }, [connection, player, playerReady, roomId]);


  useEffect(() => {
      if (playerReady && player && pendingVideo) {
      if (pendingVideo.videoId) player.loadVideoById(pendingVideo.videoId);
      if (pendingVideo.seek != null) player.seekTo(pendingVideo.seek, true);
      setPendingVideo(null);
    }
  }, [playerReady, player, pendingVideo]);

  // === 5. Handlers ===
  const handleLeaveRoom = () => {
    fetch(`${API_URL}/${roomId}/leave`, { method: "POST" })
      .then(() => navigate("/partyrooms"))
      .catch((err) => alert(err.message));
  };

  const handleSendMessage = () => {
    if (newMessage.trim()) {
      connection.invoke("SendMessage", roomId, "User", newMessage);
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
        Members: {room.guestsCount} / {room.capacity}
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
                {room.queue && room.queue.length > 0 ? (
                  room.queue.map((track, i) => {
                    // Show placeholder if the track is a placeholder
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
                  })
                ) : (
                  <li>No tracks yet</li>
                )}
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
                            connection.connectionId, // using connectionId as user
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
                            connection.connectionId,
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
