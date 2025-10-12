import { Routes, Route, useNavigate } from 'react-router-dom'
import './App.css'
import PartyRoomList from "./components/PartyRoomList";
import PartyRoomPage from "./components/PartyRoomPage";

function HomePage() {
  const navigate = useNavigate();

  return (
    <div className="homepage">
      <a href="/login" className="login-link">LOG IN</a>

      <div className="home-content">
        <h1>Welcome to CoTunes</h1>
        <p>Connect with friends and enjoy music together.</p>
        <button className="get-started-btn" onClick={() => navigate("/partyrooms")}>
          Get Started
        </button>
      </div>

      <div className="support">
        <span>Support us:</span>
        <a href="https://github.com/pakshys/psi-2025" target="_blank" rel="noreferrer">
          <img src={githubMark} alt="GitHub" width={30} height={30} />
        </a>
      </div>
    </div>
  );
} 

function App() {
  return (
    <Routes>
      <Route path = "/" element = {<HomePage/>} />
      <Route path="/login" element={<Login />} />
      <Route path = "/partyrooms" element = {<PartyRoomList/>} />
      <Route path = "/room/:id" element = {<PartyRoomPage/>} />
    </Routes>
  );
}

export default App;

