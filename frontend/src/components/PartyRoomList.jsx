import { useState, useEffect, use } from "react";
import { useNavigate } from "react-router-dom";
import "./PartyRoomList.css";

const API_URL = "https://localhost:7234/partyroom";

function PartyRoomList() {
  const [rooms, setRooms] = useState([]);
  const navigate = useNavigate();

  useEffect(() => {
    fetch(API_URL)
      .then((response) => response.json())
      .then((data) => setRooms(data))
      .catch((error) => console.error("Error fetching party rooms:", error));
  }, []);

  const handleJoinRoom = (roomId) => {
    fetch(`${API_URL}/${roomId}/join`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
    })
      .then((response) => {
        if (!response.ok) throw new Error("Failed to join room");
        return response.json();
      })
      .then(() => navigate(`/room/${roomId}`))
      .catch((error) => alert(error.message));
  };

  const handleCreateRoom = () => {
    const name = prompt("Enter room name:");
    if (!name) return;

    fetch(`${API_URL}`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ name, capacity: 10, isPrivate: false }),
    })
      .then((response) => {
        if (!response.ok) throw new Error("Failed to create room");
        return response.json();
      })
      .then((newRoom) => navigate(`/room/${newRoom.id}`))
      .catch((err) => alert(err.message));
  };

  return (
    <div className="partyroom-page">
    <div className="partyrooms-section">
      <h2>Party Rooms</h2>
      <div className="buttons">
        <button className="main-button" onClick={handleCreateRoom}>Create Room</button>
        <button className="join-button" onClick={() => alert("Select a room below to join")}>Join a Room</button>
      </div>

      <ul>
        {rooms.map((room) => (
          <li key={room.id}>
            <button
              className="join-room-button"
              onClick={() => handleJoinRoom(room.id)}
            >
              <strong>{room.name}</strong> - Capacity: {room.capacity} -{" "}
              {room.isPrivate ? "Private" : "Public"}
            </button>
          </li>
        ))}
      </ul>
    </div>

    <div className="friendrooms-section">
      <h3>Friend Rooms:</h3>
      <div className="friend-grid">
        {/* Placeholder friends (mock data) */}
        {[1, 2, 3, 4].map((i) => (
          <div key={i} className="friend-circle">
            <span>Friend {i}</span>
          </div>
        ))}
      </div>
    </div>
  </div>
  );
}

export default PartyRoomList;
