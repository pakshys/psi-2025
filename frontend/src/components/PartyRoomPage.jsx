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
      .withAutomaticReconnect()
      .build();

    conn.start()
      .then(() => {
        console.log("Connected to SignalR hub");
        conn.invoke("JoinRoom", roomId);
      })
      .catch((err) => console.error("SignalR error:", err));

    setConnection(conn);

    return () => {
      conn.invoke("LeaveRoom", roomId);
      conn.stop();
    };
  }, [roomId]);

  // === 3. Load YouTube iframe API ===
  useEffect(() => {
    if (!window.YT) {
      const tag = document.createElement("script");
      tag.src = "https://www.youtube.com/iframe_api";
      document.body.appendChild(tag);
    }

    window.onYouTubeIframeAPIReady = () => {
      const ytPlayer = new window.YT.Player("player", {
        height: "360",
        width: "640",
        videoId: "", // blank until loaded
        playerVars: { autoplay: 0, controls: 1 },
        events: {
          onReady: () => setPlayer(ytPlayer),
        },
      });
    };
  }, []);

  // === 4. Hook SignalR events ===
  useEffect(() => {
    if (!connection || !player) return;

    connection.on("LoadVideo", (videoId) => {
      console.log("Load video:", videoId);
      player.loadVideoById(videoId);
    });

    connection.on("Play", () => {
      console.log("Play event received");
      player.playVideo();
    });

    connection.on("Pause", () => {
      console.log("Pause event received");
      player.pauseVideo();
    });

    return () => {
      connection.off("LoadVideo");
      connection.off("Play");
      connection.off("Pause");
    };
  }, [connection, player]);

  // === 5. Simple UI actions ===
  const handleLeaveRoom = () => {
    fetch(`${API_URL}/${roomId}/leave`, { method: "POST" })
      .then(() => navigate("/partyrooms"))
      .catch((err) => alert(err.message));
  };

  const handleLoadClick = () => {
    const videoId = prompt("Enter YouTube Video ID:");
    if (videoId) connection.invoke("LoadVideo", roomId, videoId);
  };

  const handlePlayClick = () => connection.invoke("Play", roomId);
  const handlePauseClick = () => connection.invoke("Pause", roomId);

  // === 6. Render UI ===
  if (!room) return <p>Loading room...</p>;

  return (
      <div className="partyroom-page">
    <div className="partyroom-header">
      <h2>{room.name}</h2>
      <div>
        Members: {room.guestsCount} / {room.capacity}
        <button className="leave-room-button" onClick={handleLeaveRoom}>
          Leave Room
        </button>
      </div>
    </div>

    <p className="partyroom-info">
      {room.isPrivate ? "Private" : "Public"} room
    </p>

    <div id="player"></div>

    <div className="partyroom-buttons">
      <button onClick={handleLoadClick}>Load Video</button>
      <button onClick={handlePlayClick}>Play</button>
      <button onClick={handlePauseClick}>Pause</button>
    </div>
  </div>
  );
}
