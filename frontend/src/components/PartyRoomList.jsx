import { useState, useEffect } from "react";
import { useNavigate } from "react-router-dom";
import "./PartyRoomList.css";

const API_URL = "https://localhost:7234/partyroom";

function PartyRoomList() {
  const [rooms, setRooms] = useState([]);
  const [search, setSearch] = useState("");
  const navigate = useNavigate();

  // === Fetch room list ===
  useEffect(() => {
    const fetchRooms = () => {
      fetch(API_URL)
        .then((response) => response.json())
        .then((data) => setRooms(data))
        .catch((error) => console.error("Error fetching party rooms:", error));
    };

    fetchRooms();

    // refresh every few seconds for live updates
    const interval = setInterval(fetchRooms, 5000);
    return () => clearInterval(interval);
  }, []);

  // === Actions ===
  const handleJoinRoom = (roomId) => {
    fetch(`${API_URL}/${roomId}/join`, { method: "POST" })
      .then((r) => r.json())
      .then(() => navigate(`/room/${roomId}`))
      .catch((e) => alert(e.message));
  };

  const handleCreateRoom = () => {
    const name = prompt("Enter room name:");
    if (!name) return;

    fetch(API_URL, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ name, capacity: 10, isPrivate: false }),
    })
      .then((r) => {
        if (!r.ok) throw new Error("Failed to create room");
        return r.json();
      })
      .then((newRoom) => navigate(`/room/${newRoom.id}`))
      .catch((err) => alert(err.message));
  };

  // === Search Filter ===
  const filteredRooms = rooms.filter((r) =>
    r.name?.toLowerCase().includes(search.toLowerCase())
  );

  // === UI ===
  return (
    <div className="partyroom-page">
      <div className="partyrooms-section">
        <h2>Party Rooms</h2>
        <div className="buttons">
          <button className="main-button" onClick={handleCreateRoom}>
            Create Room
          </button>
        </div>

        <input
          type="text"
          className="search-bar"
          placeholder="Search rooms..."
          value={search}
          onChange={(e) => setSearch(e.target.value)}
        />

        <div className="room-list">
          {filteredRooms.length === 0 ? (
            <p className="no-rooms">No rooms found</p>
          ) : (
            <ul>
              {filteredRooms.map((room) => (
                <li key={room.id}>
                  <button
                    className="join-room-button"
                    onClick={() => handleJoinRoom(room.id)}
                  >
                    <strong>{room.name}</strong> — {room.guestsCount}/
                    {room.capacity} {room.isPrivate ? "🔒" : ""}
                  </button>
                </li>
              ))}
            </ul>
          )}
        </div>
      </div>

      <div className="friendrooms-section">
        <h3>Friend Rooms:</h3>
        <div className="friend-grid">
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
