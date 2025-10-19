import React, { useEffect, useState } from "react";

export default function Profile() {
  const [profile, setProfile] = useState(null);
  const [file, setFile] = useState(null);

  // Fetch user profile on page load
  useEffect(() => {
    const fetchProfile = async () => {
      try {
        const res = await fetch("https://localhost:7234/UserProfile/me", {
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
      const res = await fetch("https://localhost:7234/UserProfile/upload-picture", {
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
    <div>
      <h1>{profile.displayName || "My Profile"}</h1>

      {/* Show current profile picture */}
      {profile.profilePictureUrl ? (
        <img
          src={`https://localhost:7234/UserProfile/picture/${profile.profilePictureUrl}`}
          alt="Profile"
          width={200}
        />
      ) : (
        <p>No profile picture yet.</p>
      )}

      {/* Upload new picture */}
      <div style={{ marginTop: "20px" }}>
        <h3>Upload Profile Picture</h3>
        <input type="file" accept=".png,.jpeg,.jpg" onChange={handleFileChange} />
        <button onClick={handleUpload}>Upload</button>
      </div>
    </div>
  );
}
