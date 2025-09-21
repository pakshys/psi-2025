import { useEffect, useState } from 'react'
import reactLogo from './assets/react.svg'
import viteLogo from '/vite.svg'
import './App.css'


function App() {
  const [forecast, setForecast] = useState([]);

  useEffect(() => {
    fetch("/WeatherForecast") // thanks to Vite proxy, no need for full URL
      .then(res => {
        if (!res.ok) {
          throw new Error("Network response was not ok");
        }
        return res.json();
      })
      .then(data => setForecast(data))
      .catch(err => console.error("Fetch error:", err));
  }, []);

  return (
    <div style={{ padding: "1rem" }}>
      <h1>Weather Forecast</h1>
      {forecast.length === 0 ? (
        <p>Loading...</p>
      ) : (
        <table border="1" cellPadding="6">
          <thead>
            <tr>
              <th>Date</th>
              <th>Temp (°C)</th>
              <th>Temp (°F)</th>
              <th>Summary</th>
            </tr>
          </thead>
          <tbody>
            {forecast.map((item, index) => (
              <tr key={index}>
                <td>{new Date(item.date).toLocaleDateString()}</td>
                <td>{item.temperatureC}</td>
                <td>{item.temperatureF}</td>
                <td>{item.summary}</td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </div>
  );
}

export default App;
