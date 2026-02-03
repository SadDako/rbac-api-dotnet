import { useEffect, useMemo, useState } from "react";
import { ApiError, apiFetch } from "../api";
import { useAuth } from "../auth/AuthContext";
import Alert from "../ui/Alert";
import Button from "../ui/Button";
import Card from "../ui/Card";
import Skeleton from "../ui/Skeleton";

function decodeToken(token?: string | null) {
  if (!token) return null;
  const parts = token.split(".");
  if (parts.length < 2) return null;
  try {
    return JSON.parse(atob(parts[1].replace(/-/g, "+").replace(/_/g, "/")));
  } catch {
    return null;
  }
}

type UserDetails = {
  id: string;
  email: string;
  name: string;
  roles: string[];
  permissions: string[];
};

export default function Account() {
  const { me, token, logout } = useAuth();
  const [details, setDetails] = useState<UserDetails | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const decoded = useMemo(() => decodeToken(token), [token]);
  const exp = decoded?.exp ? new Date(decoded.exp * 1000) : null;
  const remainingMs = exp ? exp.getTime() - Date.now() : null;
  const remaining =
    remainingMs === null
      ? "-"
      : remainingMs <= 0
        ? "Expirado"
        : `${Math.floor(remainingMs / 60000)} min`;

  async function loadSession() {
    setLoading(true);
    setError(null);
    try {
      const response = await apiFetch(`/users/me`);
      const data = (await response.json()) as UserDetails;
      setDetails(data);
    } catch (err: unknown) {
      if (err instanceof ApiError) {
        setError(err.message);
      } else if (err instanceof Error) {
        setError(err.message);
      } else {
        setError("Falha ao carregar sessão.");
      }
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    loadSession();
  }, []);

  return (
    <div className="account">
      <section className="grid-2">
        <Card title="Minha Conta" description="Detalhes do usuário autenticado.">
          {loading && (
            <div className="skeleton-stack">
              <Skeleton className="skeleton-line" />
              <Skeleton className="skeleton-line" />
              <Skeleton className="skeleton-line skeleton-line--short" />
            </div>
          )}
          {!loading && error && (
            <Alert variant="error" title="Erro">
              {error}
            </Alert>
          )}
          {!loading && !error && (
            <div className="info-grid">
              <div>
                <span>Nome</span>
                <strong>{details?.name ?? me?.name ?? "-"}</strong>
              </div>
              <div>
                <span>Email</span>
                <strong>{details?.email ?? me?.email ?? "-"}</strong>
              </div>
              <div>
                <span>Roles atribuídas</span>
                <strong>{details?.roles?.join(", ") || me?.roles?.join(", ") || "-"}</strong>
              </div>
              <div>
                <span>Permissions efetivas</span>
                <strong>{details?.permissions?.join(", ") || "-"}</strong>
              </div>
            </div>
          )}
          <div className="card-actions">
            <Button variant="outline" onClick={loadSession} disabled={loading}>
              Recarregar sessão (/users/me)
            </Button>
            <Button variant="ghost" onClick={logout}>
              Logout
            </Button>
          </div>
        </Card>

        <Card title="Sessão Atual" description="Resumo do token JWT e expiração.">
          <div className="session-list">
            <div>
              <span>Expiração</span>
              <strong>{exp ? exp.toLocaleString() : "-"}</strong>
            </div>
            <div>
              <span>Tempo restante</span>
              <strong>{remaining}</strong>
            </div>
            <div>
              <span>Claims principais</span>
              <strong>{decoded ? "Carregadas" : "Indisponível"}</strong>
            </div>
          </div>
          <pre className="code-block">{decoded ? JSON.stringify(decoded, null, 2) : "Sem token"}</pre>
        </Card>
      </section>
    </div>
  );
}
