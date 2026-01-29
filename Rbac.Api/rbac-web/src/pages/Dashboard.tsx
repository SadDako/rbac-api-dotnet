import { useEffect, useMemo, useState } from "react";
import { apiFetch } from "../api";
import { useAuth } from "../auth/AuthContext";
import Card from "../ui/Card";

function parseJwtExpiration(token?: string | null) {
  if (!token) return null;
  const parts = token.split(".");
  if (parts.length < 2) return null;
  try {
    const payload = JSON.parse(atob(parts[1].replace(/-/g, "+").replace(/_/g, "/")));
    if (!payload?.exp) return null;
    return new Date(payload.exp * 1000);
  } catch {
    return null;
  }
}

export default function Dashboard() {
  const { me, token } = useAuth();
  const [apiMe, setApiMe] = useState<any>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  useEffect(() => {
    setLoading(true);
    setError("");
    apiFetch("/test/me")
      .then((r) => r.json())
      .then((data) => setApiMe(data))
      .catch((err: any) => setError(err?.message || "Não foi possível carregar seus dados"))
      .finally(() => setLoading(false));
  }, []);

  const expDate = useMemo(() => parseJwtExpiration(token), [token]);

  return (
    <div className="dashboard">
      <section className="stats-grid">
        <Card title="Status da sessão" description="Seu acesso está ativo.">
          <div className="stat">
            <span className="stat__label">Autenticação</span>
            <strong className="stat__value">Ativa</strong>
            <p className="stat__helper">Protegido com JWT</p>
          </div>
        </Card>
        <Card title="Token expira" description="Validade estimada do token atual.">
          <div className="stat">
            <span className="stat__label">Expiração</span>
            <strong className="stat__value">
              {expDate ? expDate.toLocaleString() : "Indisponível"}
            </strong>
            <p className="stat__helper">Renove o login se necessário.</p>
          </div>
        </Card>
        <Card title="Perfil" description="Resumo do usuário autenticado.">
          <div className="stat">
            <span className="stat__label">Nome</span>
            <strong className="stat__value">{me?.name ?? "Usuário"}</strong>
            <p className="stat__helper">{me?.email ?? ""}</p>
          </div>
        </Card>
      </section>

      <section className="grid-2">
        <Card title="Minhas roles" description="Papéis atribuídos no RBAC.">
          <div className="pill-group">
            {(me?.roles?.length ? me.roles : ["Sem roles"]).map((role) => (
              <span key={role} className="pill">
                {role}
              </span>
            ))}
          </div>
        </Card>

        <Card title="Minha conta" description="Informações básicas armazenadas localmente.">
          <dl className="info-list">
            <div>
              <dt>Nome</dt>
              <dd>{me?.name ?? "Usuário"}</dd>
            </div>
            <div>
              <dt>Email</dt>
              <dd>{me?.email ?? "-"}</dd>
            </div>
            <div>
              <dt>Token</dt>
              <dd className="mono">{token ? `${token.slice(0, 14)}...` : "-"}</dd>
            </div>
          </dl>
        </Card>
      </section>

      <Card
        title="Retorno da API /test/me"
        description="Dados vindos diretamente do backend."
        className="code-card"
      >
        {loading && <div className="alert">Carregando dados...</div>}
        {error && <div className="alert alert--error">{error}</div>}
        {!loading && !error && (
          <pre className="code-block">{JSON.stringify(apiMe, null, 2)}</pre>
        )}
      </Card>
    </div>
  );
}
