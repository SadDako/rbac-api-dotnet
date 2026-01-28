import { useState } from "react";
import { apiFetch } from "../api";

export default function Login({ onLogged }: { onLogged: () => void }) {
  const [email, setEmail] = useState("admin@rbac.local");
  const [password, setPassword] = useState("Admin@123");
  const [error, setError] = useState("");
  const [loading, setLoading] = useState(false);

  async function submit(e: React.FormEvent) {
    e.preventDefault();
    setError("");
    setLoading(true);
    try {
      const res = await apiFetch("/auth/login", {
        method: "POST",
        body: JSON.stringify({ email, password }),
      });
      const data = await res.json();
      localStorage.setItem("token", data.token);
      localStorage.setItem("me", JSON.stringify(data));
      onLogged();
    } catch (err: any) {
      setError(err?.message || "Erro ao logar");
    } finally {
      setLoading(false);
    }
  }

  return (
    <div style={{ padding: 24, fontFamily: "system-ui", maxWidth: 420 }}>
      <h1>Login</h1>

      <form onSubmit={submit}>
        <div style={{ marginBottom: 12 }}>
          <label>Email</label>
          <input
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            style={{ width: "100%", padding: 10 }}
          />
        </div>

        <div style={{ marginBottom: 12 }}>
          <label>Senha</label>
          <input
            type="password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            style={{ width: "100%", padding: 10 }}
          />
        </div>

        <button disabled={loading} style={{ padding: 10, width: "100%" }}>
          {loading ? "Entrando..." : "Entrar"}
        </button>

        {error && (
          <p style={{ marginTop: 12, color: "crimson" }}>{error}</p>
        )}
      </form>
    </div>
  );
}
