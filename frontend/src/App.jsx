import { Routes, Route, useNavigate } from 'react-router-dom'
import './App.css'
import Login from "./components/Login";
import Register from "./components/Register";
import PartyRoomList from "./components/PartyRoomList";
import PartyRoomPage from "./components/PartyRoomPage";
import Friends from "./components/Friends";
import githubMark from "./assets/github-mark.svg";
import UserMenu from "./components/UserMenu";
import Profile from './components/Profile';

function HomePage() {
    const navigate = useNavigate();

    return (
        <div className="homepage">
            <div className="top-right">
                <UserMenu />
            </div>

      <div className="home-content">
        <h1>Welcome to CoTunes</h1>
        <p>Connect with friends and enjoy music together.</p>
        <button className="get-started-btn" onClick={() => navigate("/partyrooms")}>
          Get Started
        </button>
        <button className="get-started-btn" onClick={() => navigate("/friends")}>
          Go to Friends
        </button>
      </div>

            <div className="support">
                <span>Support us:</span>
                <a href="https://www.pay.gov/public/form/start/708094624" target="_blank" rel="noreferrer">
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
      <Route path="/register" element={<Register />} />
      <Route path = "/partyrooms" element = {<PartyRoomList/>} />
      <Route path="/room/:id" element={<PartyRoomPage />} />
      <Route path="/profile" element={<Profile />} />
      <Route path="/friends" element={<Friends />} />
    </Routes>
  );
}

export default App;

