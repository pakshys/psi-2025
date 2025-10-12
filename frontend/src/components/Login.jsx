import React from "react";
import "./Login.css";
//import illustration from "../assets/login-art.webp"; // your image file

export default function Login() {
  return (
    <div className="login-page">
      <div className="login-form-section">
        <div className="login-form">
          <h1>Hello Again!</h1>
          <p>Let's get started with your 30-day trial</p>

          <input type="email" className="login-input" placeholder="Email" />
          <input type="password" className="login-input" placeholder="Password" />
          <a href="#" className="recovery-link">Recovery Password</a>
          <button className="login-btn">Sign In</button>

          <div className="social-login">
            <button>G</button>
            <button></button>
            <button>f</button>
          </div>
        </div>

        {/* Background shape */}
        <div className="placeholder"></div>
      </div>
    </div>
  );
}
