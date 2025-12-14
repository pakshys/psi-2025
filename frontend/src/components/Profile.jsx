import React, { useEffect, useState } from "react";
import "./Profile.css";

const API_URL = (import.meta.env.VITE_API_URL ?? "https://api.cotunes.online");
export default function Profile() {
  const [profile, setProfile] = useState(null);
  const [file, setFile] = useState(null);
  const [message, setMessage] = useState("");
  const [error, setError] = useState("");

  // Fetch user profile on page load
  useEffect(() => {
    const fetchProfile = async () => {
      try {
        const res = await fetch(`${API_URL}/UserProfile/me`, {
          credentials: "include", // send cookies
        });
        if (!res.ok) {
          console.error("Failed to load profile");
          return;
        }
        const data = await res.json();
        setProfile(data);
      } catch (err) {
        console.error("Error fetching profile:", err);
      }
    };

    fetchProfile();
  }, []);

  const handleFileChange = (e) => {
    const selectedFile = e.target.files[0];
    if (!selectedFile) return;

    // Only allow PNG or JPEG
    const allowedTypes = ["image/png", "image/jpeg"];
    if (!allowedTypes.includes(selectedFile.type)) {
      alert("Only PNG and JPEG files are allowed.");
      return;
    }

    setFile(selectedFile);
  };

  const handleUpload = async () => {
    if (!file) return;

    const formData = new FormData();
    formData.append("profile", file);

    try {
      const res = await fetch(`${API_URL}/UserProfile/upload-picture`, {
        method: "POST",
        body: formData,
        credentials: "include", // send cookies
      });

      if (!res.ok) throw new Error("Upload failed");

      const data = await res.json();

      // Update profile state with new picture URL
      setProfile((prev) => ({
        ...prev,
        profilePictureUrl: data.profilePictureUrl,
      }));

      setFile(null); // clear file input
    } catch (err) {
      console.error(err);
      alert("Upload failed");
    }
  };

  if (!profile) return <p>Loading profile...</p>;

  return (
    <div className="profile-page">
      <div className="profile-section">
        <div className="profile-card">
          <h1>{profile.displayName || "My Profile"}</h1>
          <p className="profile-subtext">Manage your account details below.</p>

          <div className="profile-picture">
            {profile.profilePictureUrl ? (
              <img
                src={`${API_URL}/UserProfile/picture/${profile.profilePictureUrl}`}
                alt="Profile"
              />
            ) : (
              <div className="no-picture">No profile picture yet</div>
            )}
          </div>

          <div className="upload-area">
            <input type="file" accept=".png,.jpeg,.jpg" onChange={handleFileChange} />
            <button onClick={handleUpload} className="upload-btn">
              Upload Picture
            </button>
          </div>

          {message && <p className="success-message">{message}</p>}
          {error && <p className="error-message">{error}</p>}

          <div className="back-link">
            <a href="/">← Back to Home</a>
          </div>
        </div>

        <div className="profile-placeholder"></div>
      </div>
    </div>
  );
}
