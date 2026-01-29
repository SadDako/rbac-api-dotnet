import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { apiFetch } from "../api";
import { useAuth } from "../auth/AuthContext";

export default function Login() {
  const nav = useNavigate();
  const { login } = useAuth();

  const [email, setEmail] = useState("admin@rbac.local");
  const [password, setPassword] = useState("Admin@123");
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");

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
      login(data);
      nav("/", { replace: true });
    } catch (err: any) {
      setError(err?.message || "Erro ao entrar");
    } finally {
      setLoading(false);
    }
  }

  return (
    <div className="auth">
      <div className="auth-card">
        <div className="auth-head">
          <div className="logo big" />
          <h1>Entrar</h1>
          <p>Acesse o painel do RBAC</p>
        </div>

        <form onSubmit={submit} className="auth-form">
          <label>
            Email
            <input value={email} onChange={(e) => setEmail(e.target.value)} />
          </label>

          <label>
            Senha
            <input type="password" value={password} onChange={(e) => setPassword(e.target.value)} />
          </label>

          {error && <div className="alert">{error}</div>}

          <button className="btn" disabled={loading}>
            {loading ? "Entrando" : "Entrar"}
          </button>
        </form>
      </div>
    </div>
  );
}
