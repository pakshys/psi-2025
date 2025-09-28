import { useNavigate, useParams } from "react-router-dom";
import { useState, useEffect } from "react";

const API_URL = "https://localhost:7234/partyroom";

function RoomPage() {
  const { id } = useParams();
  const [room, setRoom] = useState(null);
  const navigate = useNavigate();

  useEffect(() => {
    fetch(`${API_URL}/${id}`)
      .then((response) => response.json())
      .then((data) => setRoom(data))
      .catch((error) => console.error("Error fetching room:", error));
  }, [id]);

  if (!room) {
    return <p>Loading room...</p>;
  }

  const handleLeaveRoom = (roomId) => {
    fetch(`${API_URL}/${roomId}/leave`, {
    method: "POST",
    headers: { "Content-Type": "application/json" }
  })
    .then((response) => {
      if (!response.ok) {
        return response.json().then((error) => {
          throw new Error(error.error || "Failed to leave room");
        });
      }
      return response.json();
    })
    .then(() => {
      navigate(`/partyrooms`);
    })
    .catch((error) => {
      alert(error.message);
    });
  };

  return (
    <div style={{ padding: "2rem", minHeight: "100vh" }}>
      <div style={{ position: "absolute", top: "1rem", right: "1rem" }}>
        Members: {room.guestsCount} / {room.capacity}
        <button className="leave-room-button" onClick={() => handleLeaveRoom(room.id)}>Leave Room</button>
      </div>
      <h2>{room.name}</h2>
      <p>{room.isPrivate ? "Private" : "Public"} room</p>
    </div>
  );
}

export default RoomPage;
