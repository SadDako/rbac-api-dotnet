import { useMemo, useState } from "react";
import { useNavigate } from "react-router-dom";
import { ApiError, apiFetch } from "../api";
import { useAuth } from "../auth/AuthContext";
import Button from "../ui/Button";
import Input from "../ui/Input";
import Spinner from "../ui/Spinner";
import Toast from "../ui/Toast";

export default function Login() {
  const nav = useNavigate();
  const { login } = useAuth();

  const [email, setEmail] = useState("admin@rbac.local");
  const [password, setPassword] = useState("Admin@123");
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");
  const [toast, setToast] = useState("");

  const validateForm = useMemo(() => {
    if (!email.trim()) return "Informe o email.";
    if (!email.includes("@")) return "Informe um email válido.";
    if (!password.trim()) return "Informe a senha.";
    if (password.trim().length < 6) return "A senha deve ter ao menos 6 caracteres.";
    return "";
  }, [email, password]);

  async function submit(e: React.FormEvent) {
    e.preventDefault();
    setError("");
    setToast("");

    const validation = validateForm;
    if (validation) {
      setError(validation);
      return;
    }

    setLoading(true);

    try {
      const res = await apiFetch("/auth/login", {
        method: "POST",
        body: JSON.stringify({ email, password }),
      });
      const data = await res.json();
      login(data);
      nav("/", { replace: true });
    } catch (err: unknown) {
      if (err instanceof ApiError) {
        setError(err.message);
        if (err.code === "NETWORK") {
          setToast("API indisponível. Verifique se o backend está online.");
        }
      } else if (err instanceof Error) {
        setError(err.message || "Erro ao entrar");
      } else {
        setError("Erro ao entrar");
      }
    } finally {
      setLoading(false);
    }
  }

  return (
    <div className="auth">
      <div className="auth-card">
        <div className="auth-head">
          <div className="logo big" />
          <div>
            <h1>Entrar</h1>
            <p>Use suas credenciais para acessar o painel.</p>
          </div>
        </div>

        <form onSubmit={submit} className="auth-form">
          <Input
            label="Email"
            type="email"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            placeholder="voce@empresa.com"
            autoComplete="username"
            error={error && !email.trim() ? "Email é obrigatório." : undefined}
            required
          />

          <Input
            label="Senha"
            type="password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            placeholder="••••••••"
            autoComplete="current-password"
            error={error && !password.trim() ? "Senha é obrigatória." : undefined}
            required
          />

          {error && <div className="alert alert--error">{error}</div>}

          <Button type="submit" disabled={loading}>
            {loading ? (
              <>
                <Spinner size="sm" /> Entrando...
              </>
            ) : (
              "Entrar"
            )}
          </Button>
        </form>

        <div className="auth-footer">
          <div className="helper">
            <span>Ambiente local:</span>
            <strong>http://localhost:5173</strong>
          </div>
        </div>
      </div>
      {toast && (
        <div className="toast-stack">
          <Toast variant="warning" title="Sem conexão" onClose={() => setToast("")}>
            {toast}
          </Toast>
        </div>
      )}
    </div>
  );
}
