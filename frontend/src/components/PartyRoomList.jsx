import { useState, useEffect } from "react";
import { useNavigate } from "react-router-dom";
import "./PartyRoomList.css";
import CreateRoomModal from "./CreateRoomModal";
import JoinRoomModal from "./JoinRoomModal";

// const API_URL = "https://localhost:7234/partyroom";
const API_URL = (import.meta.env.VITE_API_URL ?? "https://api.cotunes.online") + "/partyroom";

function PartyRoomList() {
  const [rooms, setRooms] = useState([]);
  const [search, setSearch] = useState("");
  const [showCreateModal, setShowCreateModal] = useState(false);
  const [showJoinModal, setShowJoinModal] = useState(false);
  const [selectedRoom, setSelectedRoom] = useState(null);
  const [joinError, setJoinError] = useState("");
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
  const handleBackToMain = () => {
    navigate("/");
  };

  const handleJoinRoom = (room) => {
    setJoinError("");

    if (room.isPrivate) {
      setSelectedRoom(room);
      setShowJoinModal(true);
    } else {
      joinRoom(room.id);
    }
  };

  const joinRoom = async (roomId, password) => {
    try {
      const options = {
        method: "POST"
      };

      if (password) {
        options.headers = { "Content-Type": "application/json" };
        options.body = JSON.stringify({ password });
      }

      const response = await fetch(`${API_URL}/${roomId}/join`, options);

      if (!response.ok) {
        const data = await response.json();
        throw new Error(data.error || "Failed to join room");
      }

      navigate(`/room/${roomId}`);
    } catch (err) {
      setJoinError(err.message);
    }
  };



  const handleCreateRoom = ({ name, capacity, isPrivate, password }) => {
    if (!name.trim()) {
      alert("Room name is required");
      return;
    }

    fetch(API_URL, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({
        name,
        capacity,
        isPrivate,
        password
      })
    })
      .then((r) => {
        if (!r.ok) throw new Error("Failed to create room");
        return r.json();
      })
      .then((newRoom) => {
        setShowCreateModal(false);
        navigate(`/room/${newRoom.id}`);
      })
      .catch((err) => alert(err.message));
  };



  // === Search Filter ===
  const filteredRooms = rooms.filter((r) =>
    r.name?.toLowerCase().includes(search.toLowerCase())
  );

  // === UI ===
  return (
  <div className="partyroom-page">
    {/* Row container */}
    <div className="partyroom-layout">
      
      {/* Left: Party Rooms */}
      <div className="partyrooms-section">
        <h2>Party Rooms</h2>

        <div className="buttons">
          <button className="main-button" onClick={handleBackToMain}>
            ← Back
          </button>
          <button
            className="main-button"
            onClick={() => setShowCreateModal(true)}
          >
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
                    onClick={() => handleJoinRoom(room)}
                  >
                    <strong>{room.name}</strong> — {room.members.length}/
                    {room.capacity} {room.isPrivate ? "🔒" : ""}
                  </button>
                </li>
              ))}
            </ul>
          )}
        </div>
      </div>

      {/* Right: Friends */}
      <div className="friendrooms-section">
        <h3>Friend Rooms:</h3>
        <div className="friend-grid">
          {[1, 2, 3, 4].map((i) => (
            <div key={i} className="friend-circle">
              Friend {i}
            </div>
          ))}
        </div>
      </div>

    </div>

    {/* Modals */}
    <CreateRoomModal
      isOpen={showCreateModal}
      onClose={() => setShowCreateModal(false)}
      onCreate={handleCreateRoom}
    />
    <JoinRoomModal
      isOpen={showJoinModal}
      roomName={selectedRoom?.name}
      error={joinError}
      onClose={() => {
        setShowJoinModal(false);
        setSelectedRoom(null);
        setJoinError("");
      }}
      onSubmit={(password) => joinRoom(selectedRoom.id, password)}
    />
  </div>
);
}

export default PartyRoomList;
