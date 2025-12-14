import React, { useState, useEffect } from "react";
import { useNavigate } from "react-router-dom";
import "./UserMenu.css";

const API_URL = (import.meta.env.VITE_API_URL ?? "https://api.cotunes.online");
export default function UserMenu() {
  const [isLoggedIn, setIsLoggedIn] = useState(false);
  const [checkedAuth, setCheckedAuth] = useState(false);
  const [showDropdown, setShowDropdown] = useState(false);
  const navigate = useNavigate();

  useEffect(() => {
    fetch(`${API_URL}/Account/Me`, {
      credentials: "include"
    })
      .then(res => {
        setIsLoggedIn(res.ok);
        setCheckedAuth(true);
      })
      .catch(() => {
        setIsLoggedIn(false);
        setCheckedAuth(true);
      });
  }, []);

  const handleLogout = async () => {
    await fetch(`${API_URL}/Account/Logout`, {
      method: "POST",
      credentials: "include"
    });
    setIsLoggedIn(false);
    navigate("/");
  };

  if (!checkedAuth) return null;


  if (!isLoggedIn) {
    return (
      <>
        <a href="/login" className="login-link">LOG IN</a>
        <a href="/register" className="register-link">REGISTER</a>
      </>
    );
  }

  // Logged-in profile button
  return (
    <div className="profile-container">
      <button
        className="profile-button" // reuse login-link CSS for identical style
        onClick={() => setShowDropdown(!showDropdown)}
      >
        PROFILE
      </button>

      {showDropdown && (
        <div className="dropdown">
          <button onClick={() => navigate("/profile")} className="dropdown-item">
            My Profile
          </button>
          <button onClick={() => navigate("/settings")} className="dropdown-item">
            Settings
          </button>
          <button onClick={handleLogout} className="dropdown-item">
            Logout
          </button>
        </div>
      )}
    </div>
  );
}
