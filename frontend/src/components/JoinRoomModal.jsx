import { useEffect, useState } from "react";
import "./JoinRoomModal.css";

export default function JoinRoomModal({
  isOpen,
  onClose,
  onSubmit,
  roomName,
  error
}) {
  const [password, setPassword] = useState("");

  useEffect(() => {
    if (!isOpen) {
      setPassword("");
    }
  }, [isOpen]);

  if (!isOpen) return null;

  const handleSubmit = (e) => {
    e.preventDefault();
    onSubmit(password);
  };

  return (
    <div className="modal-overlay" onClick={onClose}>
      <div
        className="modal-form-section"
        onClick={(e) => e.stopPropagation()}
      >
        <div className="modal-form">
          <h2>Join Private Room</h2>

          <p>
            <strong>{roomName}</strong> requires a password
          </p>

          {/* Error message */}
          {error && <div className="error-message">{error}</div>}

          <form onSubmit={handleSubmit}>
            <input
              type="password"
              placeholder="Enter room password"
              className="input-box"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              required
              autoFocus
            />

            <button type="submit" className="create-btn">
              Join
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
