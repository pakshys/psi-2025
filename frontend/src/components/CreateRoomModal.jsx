import React, { useEffect, useState } from "react";
import "./CreateRoomModal.css";

export default function CreateRoomModal({ isOpen, onClose, onCreate }) {
  const [formData, setFormData] = useState({
    name: "",
    capacity: 10,
    access: "Public"
  });

  useEffect(() => {
    const handleKeyDown = (e) => {
      if (e.key === "Escape") onClose();
    };
    if (isOpen) window.addEventListener("keydown", handleKeyDown);
    return () => window.removeEventListener("keydown", handleKeyDown);
  }, [isOpen, onClose]);

  useEffect(() => {
    if (isOpen) document.body.style.overflow = "hidden";
    else document.body.style.overflow = "";
    return () => { document.body.style.overflow = ""; };
  }, [isOpen]);

  if (!isOpen) return null;

  const handleChange = (e) => {
    const { name, value } = e.target;
    setFormData({
      ...formData,
      [name]: value
    });
  };

  const handleSubmit = (e) => {
    e.preventDefault();
    onCreate({
      ...formData,
      isPrivate: formData.access === "Private"
    });
  };

  return (
    <div className="modal-overlay" onClick={onClose}>
      <div className="modal-form-section" onClick={(e) => e.stopPropagation()}>
        <div className="modal-form">
          <h1>Create Party Room</h1>

          <form onSubmit={handleSubmit}>
            <label>
              Room Name
              <input
                type="text"
                name="name"
                className="input-box"
                value={formData.name}
                onChange={handleChange}
                required
              />
            </label>

            <label>
              Room Capacity
              <input
                type="number"
                name="capacity"
                className="input-box"
                value={formData.capacity}
                onChange={handleChange}
                min={2}
                max={50}
                required
              />
            </label>

            <label>
              Room Access
              <select
                name="access"
                value={formData.access}
                onChange={handleChange}
                className="dropdown-box"
              >
                <option value="Public">Public</option>
                <option value="Private">Private</option>
              </select>
            </label>

            <button type="submit" className="create-btn">
              Create
            </button>

            <button
              type="button"
              className="cancel-btn"
              onClick={onClose}
            >
              Cancel
            </button>
          </form>
        </div>

        <div className="placeholder"></div>
      </div>
    </div>
  );
}
