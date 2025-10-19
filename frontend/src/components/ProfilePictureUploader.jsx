import React, { useState } from "react";

const ProfilePictureUploader = () => {
  const [file, setFile] = useState(null);
  const [imageUrl, setImageUrl] = useState(null);

  const handleFileChange = (e) => setFile(e.target.files[0]);

  const handleUpload = async () => {
    if (!file) return;

    const formData = new FormData();
    formData.append("profile", file);

    try {
      const res = await fetch("http://localhost:5000/UserProfile/upload-picture", {
        method: "POST",
        body: formData,
        credentials: "include" // send cookies automatically
      });

      if (!res.ok) throw new Error("Upload failed");

      const data = await res.json();
      setImageUrl(`http://localhost:5000/UserProfile/picture/${data.profilePictureUrl}`);
    } catch (err) {
      console.error(err);
      alert("Upload failed");
    }
  };

  return (
    <div>
      <h3>Upload Profile Picture</h3>
      <input type="file" onChange={handleFileChange} />
      <button onClick={handleUpload}>Upload</button>

      {imageUrl && (
        <div style={{ marginTop: "20px" }}>
          <h4>Preview:</h4>
          <img src={imageUrl} alt="Profile" width={200} />
        </div>
      )}
    </div>
  );
};

export default ProfilePictureUploader;
