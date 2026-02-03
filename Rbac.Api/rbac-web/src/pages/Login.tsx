import { useMemo, useState } from "react";
import { useNavigate } from "react-router-dom";
import { ApiError, apiFetch } from "../api";
import { useAuth } from "../auth/AuthContext";
import Badge from "../ui/Badge";
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
      <div className="auth-orbits" aria-hidden="true">
        <span />
        <span />
        <span />
      </div>
      <div className="auth-grid">
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
              aria-invalid={!!error && !email.trim()}
              icon={
                <svg viewBox="0 0 24 24" width="18" height="18" aria-hidden="true">
                  <path
                    fill="currentColor"
                    d="M4 5h16a2 2 0 0 1 2 2v10a2 2 0 0 1-2 2H4a2 2 0 0 1-2-2V7a2 2 0 0 1 2-2Zm0 2v.5l8 5 8-5V7H4Zm16 10V9.12l-7.47 4.67a2 2 0 0 1-2.06 0L3 9.12V17h17Z"
                  />
                </svg>
              }
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
              aria-invalid={!!error && !password.trim()}
              icon={
                <svg viewBox="0 0 24 24" width="18" height="18" aria-hidden="true">
                  <path
                    fill="currentColor"
                    d="M6 10V8a6 6 0 1 1 12 0v2h1a1 1 0 0 1 1 1v9a1 1 0 0 1-1 1H5a1 1 0 0 1-1-1v-9a1 1 0 0 1 1-1h1Zm2 0h8V8a4 4 0 1 0-8 0v2Zm3 4v3h2v-3h-2Z"
                  />
                </svg>
              }
              required
            />

            {error && (
              <div className="alert alert--error" role="alert">
                {error}
              </div>
            )}

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
            <Badge variant="info">API: http://localhost:5083</Badge>
          </div>
        </div>

        <aside className="auth-aside">
          <div className="auth-highlight">
            <h2>RBAC em ação</h2>
            <p>Autenticação robusta com JWT, roles e políticas de acesso.</p>
          </div>
          <div className="auth-feature-grid">
            <div className="auth-feature">
              <strong>Feedback visual</strong>
              <p>Mensagens claras para sucesso, erro e alertas de sessão.</p>
            </div>
            <div className="auth-feature">
              <strong>Experiência premium</strong>
              <p>Interface dark com microinterações suaves e responsivas.</p>
            </div>
            <div className="auth-feature">
              <strong>Pronto para demos</strong>
              <p>Front e API integrados, rodando em localhost.</p>
            </div>
          </div>
        </aside>
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
