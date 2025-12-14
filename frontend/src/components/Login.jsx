import React, { useState } from "react";
import "./Login.css";
import { useNavigate } from "react-router-dom";


const API_URL = (import.meta.env.VITE_API_URL ?? "https://api.cotunes.online");

export default function Login() {
    const navigate = useNavigate();
    const handleBack = () => {
    navigate("/"); // go to main page
    };


    const [formData, setFormData] = useState({
        login: "",
        password: "",
        rememberMe: false
    });
    const [message, setMessage] = useState("");
    const [error, setError] = useState("");

    const handleChange = (e) => {
        const { name, value, type, checked } = e.target;
        setFormData({
            ...formData,
            [name]: type === "checkbox" ? checked : value
        });
    };

    const handleSubmit = async (e) => {
        e.preventDefault();
        setMessage("");
        setError("");

        try {
            const response = await fetch(`${API_URL}/Account/Login`, {
                method: "POST",
                headers: {
                    "Content-Type": "application/json"
                },
                credentials: "include",
                body: JSON.stringify({
                    login: formData.login,
                    password: formData.password,
                    rememberMe: formData.rememberMe
                })
            });

            if (response.ok) {
                const data = await response.json();
                setMessage(data.message || "Login successful!");
                setError("");
                // Redirect after successful login
                window.location.href = "/";
            } else {
                const err = await response.json();
                setError(err.message || "Login failed.");
            }
        } catch (ex) {
            console.error(ex);
            setError("An error occurred. Please try again later.");
        }
    };

    return (
        <div className="login-page">
            <div className="login-form-section">
                <div className="login-form">
                <h1>Hello Again!</h1>
                <p>Welcome back! Please login to your account.</p>

                <form onSubmit={handleSubmit}>
                    <input
                    type="text"
                    name="login"
                    className="login-input"
                    placeholder="Email or Username"
                    value={formData.login}
                    onChange={handleChange}
                    required
                    />
                    <input
                    type="password"
                    name="password"
                    className="login-input"
                    placeholder="Password"
                    value={formData.password}
                    onChange={handleChange}
                    required
                    />
                    <div style={{ marginBottom: "10px" }}>
                    <label>
                        <input
                        type="checkbox"
                        name="rememberMe"
                        checked={formData.rememberMe}
                        onChange={handleChange}
                        />{" "}
                        Remember me
                    </label>
                    </div>

                    {error && <p className="error-message">{error}</p>}
                    {message && <p className="success-message">{message}</p>}

                    <button type="submit" className="login-btn">Sign In</button>
                </form>

                <p className="account-link">
                    Don't have an account? <a href="/register">Register</a>
                </p>

                {/* Back link below account link */}
                <button
                    type="button"
                    className="back-link"
                    onClick={handleBack}
                >
                    ← Back
                </button>
                </div>

                {/* Background shape */}
                <div className="placeholder"></div>
            </div>
        </div>
    );
}
