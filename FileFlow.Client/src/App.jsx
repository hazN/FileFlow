import "./App.css";
import { useState } from "react";
import Home from "./pages/Home";
import Login from "./pages/Login";

function App() {
    const [email, setEmail] = useState(() => localStorage.getItem("fileflow_email"));

    const handleLogout = () => {
        localStorage.removeItem("fileflow_token");
        localStorage.removeItem("fileflow_email");
        setEmail(null);
    };

    if (!email) {
        return <Login onLoggedIn={setEmail} />;
    }

    return <Home currentEmail={email} onLogout={handleLogout} />;
}

export default App;