import { useState, useEffect, use } from "react";
import "./PartyRooms.css";

const API_URL = "https://localhost:7234/partyroom";

function PartyRooms() {
  const [rooms, setRooms] = useState([]);

  useEffect(() => {
    fetch(API_URL)
      .then((response) => response.json())
      .then((data) => setRooms(data))
      .catch((error) => console.error("Error fetching party rooms:", error));
  }, []);

  return (
    <div className="partyroom-container">
      <h2>Party Rooms</h2>
      <ul>
        {rooms.map((room) => (
          <li key={room.id}>
            <strong>{room.name}</strong> - Capacity: {room.capacity} - {" "}
            {room.isPrivate ? "Private" : "Public"}
          </li>
        ))}
      </ul>
      <button className="main-button" onClick={() => alert("Create Room clicked.")}> 
        Create Room
      </button>
    </div>
  );
}

export default PartyRooms;
