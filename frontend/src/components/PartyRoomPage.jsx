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

  // Start connection immediately
  conn.start()
    .then(() => {
      console.log("✅ Connected to SignalR hub");
      conn.invoke("JoinRoom", roomId);
    })
    .catch((err) => console.error("SignalR connection failed:", err));

  conn.onreconnected(() => {
    console.log("Reconnected to SignalR hub, rejoining room...");
    conn.invoke("JoinRoom", roomId);
  });

  setConnection(conn);

  const handleBeforeUnload = () => {
    navigator.sendBeacon(`${API_URL}/${roomId}/leave`);
  };
  window.addEventListener("beforeunload", handleBeforeUnload);

  return () => {
    conn.invoke("LeaveRoom", roomId).catch(() => {});
    conn.stop();
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
    {/* Top bar spans full width */}
    <div className="partyroom-header">
      <h2>{room.name}</h2>
      <div>
        Members: {room.guestsCount} / {room.capacity}
        <button onClick={handleLeaveRoom}>Leave Room</button>
      </div>
    </div>

    {/* Main content row */}
    <div className="partyroom-main">
      <div className="partyroom-content">
        <p className="partyroom-info">
          {room.isPrivate ? "Private" : "Public"} room
        </p>

        <div id="player"></div>

        <div className="partyroom-buttons">
          <button
            onClick={() => {
              const videoId = prompt("Enter YouTube Video ID:");
              if (videoId && connection) {
                console.log("Invoking LoadVideo:", videoId);
                connection.invoke("LoadVideo", roomId, videoId);
                if (playerReady && player) player.loadVideoById(videoId);
              }
            }}
          >
            Load Video
          </button>
          <button onClick={() => connection.invoke("Play", roomId)}>Play</button>
          <button onClick={() => connection.invoke("Pause", roomId)}>
            Pause
          </button>
        </div>
      </div>

      {/* Chat on the right */}
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
            placeholder="Type a message..."
          />
          <button onClick={handleSendMessage}>Send</button>
        </div>
      </div>
    </div>
  </div>
);

}
