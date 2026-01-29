import { useEffect, useMemo, useState } from "react";
import { ApiError, apiFetch } from "../api";
import { useAuth } from "../auth/AuthContext";
import Alert from "../ui/Alert";
import Badge from "../ui/Badge";
import Card from "../ui/Card";
import Spinner from "../ui/Spinner";

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
  const [error, setError] = useState<ApiError | null>(null);

  useEffect(() => {
    setLoading(true);
    setError(null);
    apiFetch("/test/me")
      .then((r) => r.json())
      .then((data) => setApiMe(data))
      .catch((err: unknown) => {
        if (err instanceof ApiError) {
          setError(err);
        } else if (err instanceof Error) {
          setError(new ApiError(err.message));
        } else {
          setError(new ApiError("Não foi possível carregar seus dados"));
        }
      })
      .finally(() => setLoading(false));
  }, []);

  const expDate = useMemo(() => parseJwtExpiration(token), [token]);
  const roleLabel = me?.roles?.length ? me.roles.join(", ") : "Sem roles";

  return (
    <div className="dashboard">
      <section className="stats-grid">
        <Card title="Status da sessão" description="Seu acesso está ativo.">
          <div className="stat">
            <span className="stat__label">Autenticação</span>
            <strong className="stat__value">
              <Badge variant="success">Ativa</Badge>
            </strong>
            <p className="stat__helper">Protegido com JWT e roles.</p>
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
            <Badge variant="info">{roleLabel}</Badge>
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

      <section className="grid-2">
        <Card title="Minha conta (API)" description="Dados retornados pelo endpoint /test/me.">
          {loading && (
            <div className="inline-status">
              <Spinner size="sm" />
              <span>Consultando API...</span>
            </div>
          )}
          {!loading && error && (
            <Alert variant="warning" title="API indisponível">
              {error.message || "Não foi possível carregar seus dados."}
            </Alert>
          )}
          {!loading && !error && apiMe && (
            <dl className="info-list">
              <div>
                <dt>Nome</dt>
                <dd>{apiMe?.name ?? "-"}</dd>
              </div>
              <div>
                <dt>Email</dt>
                <dd>{apiMe?.email ?? "-"}</dd>
              </div>
              <div>
                <dt>Roles</dt>
                <dd>{apiMe?.roles?.join(", ") ?? "-"}</dd>
              </div>
            </dl>
          )}
        </Card>

        <Card title="Status do backend" description="Monitoramento básico de conectividade.">
          {loading && (
            <div className="inline-status">
              <Spinner size="sm" />
              <span>Verificando conexão...</span>
            </div>
          )}
          {!loading && error?.code === "NETWORK" && (
            <Alert variant="warning" title="Disconnected">
              Não foi possível alcançar a API. Tente novamente em instantes.
            </Alert>
          )}
          {!loading && !error && (
            <Alert variant="success" title="Conectado">
              Backend respondeu normalmente.
            </Alert>
          )}
        </Card>
      </section>

      <Card
        title="Retorno da API /test/me"
        description="Dados vindos diretamente do backend."
        className="code-card"
      >
        {loading && (
          <div className="inline-status">
            <Spinner size="sm" />
            <span>Carregando dados...</span>
          </div>
        )}
        {error && (
          <Alert variant="error" title="Falha ao consultar API">
            {error.message}
          </Alert>
        )}
        {!loading && !error && (
          <pre className="code-block">{JSON.stringify(apiMe, null, 2)}</pre>
        )}
      </Card>
    </div>
  );
}
