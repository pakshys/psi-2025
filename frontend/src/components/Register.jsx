import React, { useState } from "react";
import "./Register.css";

const API_URL = (import.meta.env.VITE_API_URL ?? "https://api.cotunes.online");
export default function Register() {
    const [formData, setFormData] = useState({
        userName: "",
        email: "",
        password: "",
        confirmPassword: ""
    });

    const [message, setMessage] = useState("");
    const [error, setError] = useState("");

    const handleChange = (e) => {
        setFormData({
            ...formData,
            [e.target.name]: e.target.value
        });
    };

    const handleSubmit = async (e) => {
        e.preventDefault();
        setMessage("");
        setError("");

        if (formData.password !== formData.confirmPassword) {
            setError("Passwords do not match.");
            return;
        }

        try {
          const response = await fetch(`${API_URL}/account/register`, {
                method: "POST",
                headers: {
                    "Content-Type": "application/json"
                },
                credentials: "include",
                body: JSON.stringify({
                    userName: formData.userName,
                    email: formData.email,
                    password: formData.password,        // lowercase
                    confirmPassword: formData.confirmPassword // include this
                })
            });

            if (response.ok) {
                const data = await response.json();
                setMessage(data.message || "Registration successful!");
                setFormData({ userName: "", email: "", password: "", confirmPassword: "" });

                // Optional redirect
                // window.location.href = "/";
            } else {
                const err = await response.json();

                // Show validation errors
                let errorMessage = err.message || "Registration failed.";
                if (err.errors) {
                    const validationErrors = Object.values(err.errors).flat().join(" ");
                    errorMessage += " " + validationErrors;
                }

                setError(errorMessage);
            }
        } catch (ex) {
            console.error(ex);
            setError("An error occurred. Please try again later.");
        }
    };

    return (
        <div className="register-page">
            <div className="register-form-section">
                <div className="register-form">
                    <h1>Create Account</h1>
                    <p>Join us and start your journey today!</p>

                    <form onSubmit={handleSubmit}>
                        <input
                            type="text"
                            name="userName"
                            className="register-input"
                            placeholder="Username"
                            value={formData.userName}
                            onChange={handleChange}
                            required
                        />
                        <input
                            type="email"
                            name="email"
                            className="register-input"
                            placeholder="Email"
                            value={formData.email}
                            onChange={handleChange}
                            required
                        />
                        <input
                            type="password"
                            name="password"
                            className="register-input"
                            placeholder="Password"
                            value={formData.password}
                            onChange={handleChange}
                            required
                        />
                        <input
                            type="password"
                            name="confirmPassword"
                            className="register-input"
                            placeholder="Confirm Password"
                            value={formData.confirmPassword}
                            onChange={handleChange}
                            required
                        />

                        {error && <p className="error-message">{error}</p>}
                        {message && <p className="success-message">{message}</p>}

                        <button type="submit" className="register-btn">Register</button>
                    </form>

                    <p className="account-link">
                        Already have an account? <a href="/login">Sign In</a>
                    </p>
                </div>

                <div className="placeholder"></div>
            </div>
        </div>
    );
}
