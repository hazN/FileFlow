import { useState } from "react";
import { login, register } from "../services/api";

export default function Login({ onLoggedIn }) {
    const [isRegistering, setIsRegistering] = useState(false);
    const [email, setEmail] = useState("");
    const [password, setPassword] = useState("");
    const [error, setError] = useState(null);

    const handleSubmit = async (e) => {
        e.preventDefault();
        setError(null);

        try {
            const result = isRegistering
                ? await register(email, password)
                : await login(email, password);

            localStorage.setItem("fileflow_token", result.token);
            localStorage.setItem("fileflow_email", result.email);
            onLoggedIn(result.email);
        } catch (err) {
            setError(err.message);
        }
    };

    return (
        <div className="app" style={{ maxWidth: "360px" }}>
            <h1>FileFlow</h1>
            <h3>{isRegistering ? "Create an account" : "Log in"}</h3>

            <form onSubmit={handleSubmit}>
                <div style={{ marginBottom: "10px" }}>
                    <input
                        type="email"
                        placeholder="Email"
                        value={email}
                        onChange={(e) => setEmail(e.target.value)}
                        style={{ width: "100%", padding: "8px" }}
                        required
                    />
                </div>
                <div style={{ marginBottom: "10px" }}>
                    <input
                        type="password"
                        placeholder="Password"
                        value={password}
                        onChange={(e) => setPassword(e.target.value)}
                        style={{ width: "100%", padding: "8px" }}
                        required
                    />
                </div>

                {error && <p style={{ color: "red" }}>{error}</p>}

                <button type="submit" className="btn-primary" style={{ width: "100%" }}>
                    {isRegistering ? "Register" : "Log in"}
                </button>
            </form>

            <p style={{ marginTop: "12px" }}>
                <button onClick={() => setIsRegistering(!isRegistering)}>
                    {isRegistering ? "Already have an account? Log in" : "Need an account? Register"}
                </button>
            </p>
        </div>
    );
}