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
  useEffect(() => {
    const conn = new signalR.HubConnectionBuilder()
      .withUrl("https://localhost:7234/hubs/partyroom")
      .withAutomaticReconnect([0, 2000, 5000, 10000])
      .build();

    // start and only set connection after successful start
    conn.start()
      .then(() => {
        console.log("✅ Connected to SignalR hub");
        // server expects an int for JoinRoom in your hub — pass a number
        conn.invoke("JoinRoom", parseInt(roomId)).catch(() => {});
        // then expose to handlers
        setConnection(conn);
      })
      .catch((err) => {
        console.error("SignalR connection failed:", err);
      });

    conn.onreconnected(() => {
      console.log("Reconnected to SignalR hub, rejoining room...");
      conn.invoke("JoinRoom", parseInt(roomId)).catch(() => {});
    });

    const handleBeforeUnload = () => {
      // sendBeacon to leave endpoint so server count updates if user closes tab
      navigator.sendBeacon(`${API_URL}/${roomId}/leave`);
    };
    window.addEventListener("beforeunload", handleBeforeUnload);

    return () => {
      // Ask hub to remove from group; safe-guard with try/catch
      conn.invoke("LeaveRoom", parseInt(roomId)).catch(() => {});
      conn.stop().catch(() => {});
      window.removeEventListener("beforeunload", handleBeforeUnload);
    };
  }, [roomId]);


  // === 3. Load YouTube iframe API ===
  useEffect(() => {
    function initPlayer() {
      const ytPlayer = new window.YT.Player("player", {
        height: "360",
        width: "640",
        videoId: "",
        playerVars: { autoplay: 0, controls: 1, origin: window.location.origin },
        events: {
          onReady: (e) => {
            setPlayer(e.target);
            setPlayerReady(true);
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
  }, []);


  // === 4. Hook SignalR events ===
  useEffect(() => {
    if (!connection) return;

    const safePlayerCall = (callback) => {
      if (playerReady && player) callback();
    };

    // Video controls
    connection.on("LoadVideo", (videoId) => {
    console.log("Loading video ID:", videoId);
    if (!playerReady || !player) {
      setPendingVideo({ videoId, seek: null });
    } else {
      player.loadVideoById(videoId);
    }
  });

  connection.on("SeekTo", (time) => {
    if (!playerReady || !player) {
      setPendingVideo((prev) => prev ? { ...prev, seek: time } : { videoId: null, seek: time });
    } else {
      player.seekTo(time, true);
    }
  });


    connection.on("Play", () => safePlayerCall(() => player.playVideo()));
    connection.on("Pause", () => safePlayerCall(() => player.pauseVideo()));

    // === Periodically send current video time to backend ===
    const syncInterval = setInterval(() => {
      if (connection && player && player.getCurrentTime && player.getPlayerState() === 1) {
        const currentTime = player.getCurrentTime();
        connection.invoke("UpdateTime", roomId, currentTime).catch(() => {});
      }
    }, 3000); // every 3 seconds

    // Room updates
    connection.on("PartyRoomUpdated", (updatedRoom) => {
      if (updatedRoom.id === parseInt(roomId)) {
        setRoom(updatedRoom);
      }
    });

    // Chat listener
    connection.on("ReceiveMessage", (user, message) => {
      setMessages((prev) => [...prev, { user, message }]);
    });

    return () => {
      clearInterval(syncInterval);
      connection.off("LoadVideo");
      connection.off("SeekTo"); // new
      connection.off("Play");
      connection.off("Pause");
      connection.off("PartyRoomUpdated");
      connection.off("ReceiveMessage");
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
          <p>(No tracks yet)</p>
        </div>
      </div>

      {/* === Video & Controls (Center) === */}
      <div className="partyroom-content">
        <p className="partyroom-info">
          {room.isPrivate ? "Private" : "Public"} room
        </p>

        <div id="player"></div>

        <div className="partyroom-buttons">
          <button
          onClick={async () => {
            const videoId = prompt("Enter YouTube Video ID:");
            if (!videoId) return;

            if (!connection || connection.state !== "Connected") {
              alert("Not connected to server yet. Try again in a moment.");
              return;
            }

            try {
              // Enqueue expects an int room id (your hub uses int for EnqueueTrack)
              await connection.invoke("EnqueueTrack", parseInt(roomId), videoId);

              // LoadVideo in your merged hub accepts (string roomId, string videoId) — send string
              await connection.invoke("LoadVideo", roomId.toString(), videoId);
            } catch (err) {
              console.error("Enqueue/Load failed:", err);
              alert("Failed to enqueue or load the track.");
            }
          }}
        >
          Add to Queue
        </button>

        <button
          onClick={async () => {
            if (!connection || connection.state !== "Connected") {
              alert("Not connected to server yet.");
              return;
            }
            try {
              // Play expects string roomId in your current hub — send string
              await connection.invoke("Play", roomId.toString());
            } catch (err) {
              console.error("Play invoke failed:", err);
            }
          }}
        >
          Play
        </button>

        <button
          onClick={async () => {
            if (!connection || connection.state !== "Connected") {
              alert("Not connected to server yet.");
              return;
            }
            try {
              await connection.invoke("Pause", roomId.toString());
            } catch (err) {
              console.error("Pause invoke failed:", err);
            }
          }}
        >
          Pause
        </button>
        </div>
      </div>

      {/* === Chat (Right) === */}
      <div className="chat-panel">
        <div className="chat-messages">
          {messages.map((m, i) => (
            <div key={i}>
              <strong>{m.user}:</strong> {m.message}
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
      </div>
    </div>
  </div>
);


}
