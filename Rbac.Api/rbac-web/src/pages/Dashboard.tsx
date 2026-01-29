import { useEffect, useMemo, useState } from "react";
import { ApiError, apiFetch } from "../api";
import { useAuth } from "../auth/AuthContext";
import Alert from "../ui/Alert";
import Badge from "../ui/Badge";
import Card from "../ui/Card";
import Skeleton from "../ui/Skeleton";
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
  const [latencyMs, setLatencyMs] = useState<number | null>(null);
  const [activityFeed, setActivityFeed] = useState<Array<{ id: string; label: string; at: string }>>([]);

  useEffect(() => {
    setLoading(true);
    setError(null);
    const startedAt = performance.now();
    apiFetch("/test/me")
      .then((r) => r.json())
      .then((data) => {
        setApiMe(data);
        setLatencyMs(Math.round(performance.now() - startedAt));
      })
      .catch((err: unknown) => {
        setLatencyMs(null);
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

  useEffect(() => {
    function loadActivity() {
      try {
        const stored = localStorage.getItem("rbac-activity");
        const parsed = stored ? (JSON.parse(stored) as Array<{ id: string; label: string; at: string }>) : [];
        setActivityFeed(parsed);
      } catch {
        setActivityFeed([]);
      }
    }

    loadActivity();
    const handleActivity = () => loadActivity();
    window.addEventListener("rbac-activity", handleActivity);
    return () => window.removeEventListener("rbac-activity", handleActivity);
  }, []);

  const expDate = useMemo(() => parseJwtExpiration(token), [token]);
  const roleCount = me?.roles?.length ?? 0;
  const roleLabel = roleCount ? me?.roles?.join(", ") : "Sem roles";
  const accessLevel = roleCount >= 3 ? "Elevado" : roleCount >= 1 ? "Essencial" : "Limitado";
  const securityScore = Math.min(100, 55 + roleCount * 15 + (token ? 10 : 0));

  const expirationStatus = expDate
    ? expDate.getTime() < Date.now()
      ? "Expirado"
      : "Dentro do prazo"
    : "Indisponível";

  const remainingMs = expDate ? expDate.getTime() - Date.now() : null;
  const isExpiringSoon = remainingMs !== null && remainingMs > 0 && remainingMs < 15 * 60 * 1000;
  const sessionTone: "warning" | "danger" | "success" = !expDate
    ? "warning"
    : remainingMs !== null && remainingMs <= 0
      ? "danger"
      : "success";

  function formatRemaining(date?: Date | null) {
    if (!date) return "Sem dados";
    const diff = date.getTime() - Date.now();
    if (diff <= 0) return "Expirado";
    const minutes = Math.floor(diff / 60000);
    if (minutes < 60) return `${minutes} min`;
    const hours = Math.floor(minutes / 60);
    if (hours < 24) return `${hours}h`;
    const days = Math.floor(hours / 24);
    return `${days}d`;
  }

  const rolePermissions = useMemo(() => {
    const roles = new Set((me?.roles ?? []).map((role) => role.toLowerCase()));
    return {
      admin: roles.has("admin"),
      manager: roles.has("manager") || roles.has("gestor"),
      auditor: roles.has("auditor"),
      support: roles.has("support") || roles.has("suporte"),
      viewer: roles.has("viewer") || roles.has("leitor"),
    };
  }, [me?.roles]);

  const permissionsCatalog = [
    {
      key: "access-dashboard",
      title: "Dashboard & visão geral",
      description: "Acesso aos indicadores principais e resumo da conta.",
      access: true,
    },
    {
      key: "manage-users",
      title: "Gerenciar usuários",
      description: "Criar, editar e suspender contas.",
      access: rolePermissions.admin || rolePermissions.manager,
    },
    {
      key: "audit-logs",
      title: "Auditoria & logs",
      description: "Visualizar trilhas de auditoria e exportações.",
      access: rolePermissions.admin || rolePermissions.auditor,
    },
    {
      key: "admin-panel",
      title: "Painel administrativo",
      description: "Configurar políticas RBAC e acesso avançado.",
      access: rolePermissions.admin,
    },
    {
      key: "support-tools",
      title: "Suporte & atendimento",
      description: "Ferramentas de atendimento e reset de sessão.",
      access: rolePermissions.admin || rolePermissions.support,
    },
    {
      key: "reporting",
      title: "Relatórios operacionais",
      description: "Insights de uso e métricas operacionais.",
      access: rolePermissions.admin || rolePermissions.manager || rolePermissions.viewer,
    },
  ];

  return (
    <div className="dashboard">
      <section className="dashboard-hero">
        <div className="hero-copy">
          <p className="eyebrow">RBAC Mission Control</p>
          <h2>Olá, {me?.name ?? "Usuário"}.</h2>
          <p>
            Seu acesso está protegido por JWT e políticas de acesso. Acompanhe o estado da sessão e
            os dados retornados pela API em tempo real.
          </p>
          <div className="hero-tags">
            <Badge variant="success">JWT Ativo</Badge>
            <Badge variant="info">RBAC</Badge>
            <Badge variant="warning">Local Dev</Badge>
          </div>
        </div>
        <Card className="card--glow hero-card" title="Nível de segurança">
          <div className="metric-list">
            <div className="metric">
              <span className="metric__label">Perfil</span>
              <strong className="metric__value">{accessLevel}</strong>
              <small className="metric__helper">{roleCount} role(s) aplicadas</small>
            </div>
            <div className="metric">
              <span className="metric__label">Token</span>
              <strong className="metric__value">{expirationStatus}</strong>
              <small className="metric__helper">
                {expDate ? `Expira em ${formatRemaining(expDate)}` : "Sem dados de expiração"}
              </small>
            </div>
            <div className="meter">
              <div className="meter__header">
                <span>Health score</span>
                <strong>{securityScore}%</strong>
              </div>
              <div className="meter__track">
                <span className="meter__fill" style={{ width: `${securityScore}%` }} />
              </div>
              <small>Baseado na sessão e roles ativas.</small>
            </div>
          </div>
        </Card>
      </section>

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
        <Card title="Session Health" description="Status do token e janela de expiração.">
          <div className="health-card">
            <div>
              <p className="section-label">JWT</p>
              <h4>{expirationStatus}</h4>
              <p className="muted">
                {expDate ? `Tempo restante: ${formatRemaining(expDate)}` : "Sem dados de expiração."}
              </p>
            </div>
            <div className="health-meta">
              <Badge variant={sessionTone}>{isExpiringSoon ? "Expira em breve" : "Seguro"}</Badge>
              <span>{expDate ? expDate.toLocaleTimeString() : "--:--"}</span>
            </div>
          </div>
          {isExpiringSoon && (
            <Alert variant="warning" title="Atenção">
              Seu token está próximo de expirar. Faça login novamente se necessário.
            </Alert>
          )}
        </Card>

        <Card title="API Connectivity" description="Latência estimada da API /test/me.">
          <div className="connectivity-card">
            <div>
              <p className="section-label">Status</p>
              <h4>{error ? "Instável" : "Online"}</h4>
              <p className="muted">
                {latencyMs !== null ? `Latência média: ${latencyMs}ms` : "Latência indisponível."}
              </p>
            </div>
            <div className="health-meta">
              <Badge variant={error ? "warning" : "success"}>{error ? "Com alertas" : "Saudável"}</Badge>
              <span>{latencyMs !== null ? `${latencyMs}ms` : "--"}</span>
            </div>
          </div>
        </Card>
      </section>

      <section className="grid-2">
        <Card title="Minhas roles" description="Papéis atribuídos no RBAC.">
          {roleCount ? (
            <div className="pill-group">
              {me?.roles?.map((role) => (
                <span key={role} className="pill">
                  {role}
                </span>
              ))}
            </div>
          ) : (
            <div className="empty-state">
              <strong>Nenhuma role atribuída</strong>
              <p>Solicite ao admin o acesso necessário para destravar esta área.</p>
            </div>
          )}
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
        <Card
          title="RBAC Overview"
          description="Resumo das roles e permissões derivadas (demo)."
        >
          <div className="permissions-overview">
            <div>
              <p className="section-label">Mapa de acesso</p>
              <h4>RBAC em ação</h4>
              <p className="muted">
                Sem endpoint dedicado, exibimos um mapa demonstrativo baseado nas roles atuais.
              </p>
            </div>
            <div className="permissions-summary">
              <div>
                <strong>{permissionsCatalog.filter((item) => item.access).length}</strong>
                <span>Permissões ativas</span>
              </div>
              <div>
                <strong>{permissionsCatalog.filter((item) => !item.access).length}</strong>
                <span>Restrições</span>
              </div>
            </div>
          </div>
          <div className="permissions-grid">
            {permissionsCatalog.map((permission) => (
              <div
                key={permission.key}
                className={`permission-card ${permission.access ? "is-allowed" : "is-denied"}`}
              >
                <div className="permission-header">
                  <span className="permission-dot" aria-hidden="true" />
                  <div>
                    <strong>{permission.title}</strong>
                    <p>{permission.description}</p>
                  </div>
                </div>
                <span className="permission-status">
                  {permission.access ? "Acesso liberado" : "Sem permissão"}
                </span>
              </div>
            ))}
          </div>
        </Card>

        <Card title="Narrativa RBAC" description="Resumo educativo do fluxo de acesso.">
          <div className="storyline">
            <div className="story-step">
              <span className="story-index">1</span>
              <div>
                <strong>Autenticação</strong>
                <p>Login gera um JWT e inicia a sessão segura.</p>
              </div>
            </div>
            <div className="story-step">
              <span className="story-index">2</span>
              <div>
                <strong>Roles</strong>
                <p>O backend vincula roles que definem o nível de acesso.</p>
              </div>
            </div>
            <div className="story-step">
              <span className="story-index">3</span>
              <div>
                <strong>Permissões</strong>
                <p>As políticas liberam recursos conforme as roles ativas.</p>
              </div>
            </div>
          </div>
          <Alert variant="info" title="Dica">
            Use o RBAC Overview para visualizar rapidamente o que está habilitado.
          </Alert>
        </Card>
      </section>

      <section className="grid-2">
        <Card title="Activity Feed" description="Ações recentes salvas localmente para demo.">
          {activityFeed.length === 0 ? (
            <div className="empty-state">
              <strong>Nenhuma atividade recente</strong>
              <p>Navegue pelo app para registrar eventos locais de demonstração.</p>
            </div>
          ) : (
            <ul className="activity-list">
              {activityFeed.map((item) => (
                <li key={item.id} className="activity-item">
                  <span className="activity-dot" aria-hidden="true" />
                  <div>
                    <strong>{item.label}</strong>
                    <span>{new Date(item.at).toLocaleString()}</span>
                  </div>
                </li>
              ))}
            </ul>
          )}
        </Card>

        <Card title="Minha conta (API)" description="Dados retornados pelo endpoint /test/me.">
          {loading && (
            <div className="skeleton-stack">
              <Skeleton className="skeleton-line" />
              <Skeleton className="skeleton-line" />
              <Skeleton className="skeleton-line skeleton-line--short" />
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
      </section>

      <section className="grid-2">
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
          <div className="skeleton-stack">
            <Skeleton className="skeleton-block" />
            <Skeleton className="skeleton-block skeleton-block--short" />
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
