import { Routes, Route, useNavigate } from 'react-router-dom'
import './App.css'
import PartyRooms from "./components/PartyRooms";

function HomePage() {
  const navigate = useNavigate();

  return (
    <div className = "main-container">
      <h1>Welcome to CoTunes</h1>
      <p>Connect with friends and enjoy music together.</p>
      <button className = "main-button" onClick={() => navigate("/partyrooms")}> 
        Get Started
      </button>
    </div>
  );
} 

function App() {
  return (
    <Routes>
      <Route path = "/" element = {<HomePage/>} />
      <Route path = "/partyrooms" element = {<PartyRooms/>} />
    </Routes>
  );
}

export default App;

