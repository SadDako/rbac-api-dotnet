import { useEffect, useState } from "react";
import { ApiError, apiFetch } from "../api";
import { useNavigate } from "react-router-dom";
import Alert from "../ui/Alert";
import Badge from "../ui/Badge";
import Button from "../ui/Button";
import Card from "../ui/Card";
import Skeleton from "../ui/Skeleton";
import Spinner from "../ui/Spinner";

export default function Admin() {
  const nav = useNavigate();
  const [result, setResult] = useState<any>(null);
  const [error, setError] = useState<ApiError | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    setLoading(true);
    setError(null);
    apiFetch("/admin/ping")
      .then((r) => r.json())
      .then(setResult)
      .catch((err: unknown) => {
        if (err instanceof ApiError) {
          setError(err);
        } else if (err instanceof Error) {
          setError(new ApiError(err.message));
        } else {
          setError(new ApiError("Erro ao acessar o admin"));
        }
      })
      .finally(() => setLoading(false));
  }, []);

  return (
    <div className="admin">
      <Card title="Área Administrativa" description="Painel premium para controle RBAC.">
        {loading && (
          <div className="admin-loading">
            <div className="inline-status">
              <Spinner size="sm" />
              <span>Carregando permissões...</span>
            </div>
            <div className="skeleton-stack">
              <Skeleton className="skeleton-line" />
              <Skeleton className="skeleton-line" />
              <Skeleton className="skeleton-line skeleton-line--short" />
            </div>
          </div>
        )}

        {!loading && error && (
          <Alert variant="error" title="Erro inesperado">
            {error.message}
          </Alert>
        )}

        {!loading && !error && (
          <div className="admin-grid">
            <div className="admin-callout">
              <Badge variant="success">Admin</Badge>
              <h3>Permissões carregadas</h3>
              <p>Você possui acesso ao painel administrativo.</p>
              <div className="admin-tools">
                <div>
                  <strong>Admin Tools</strong>
                  <p>Gerencie roles, políticas RBAC e segurança do tenant.</p>
                </div>
                <ul>
                  <li>Gerenciar usuários e convites</li>
                  <li>Políticas de acesso e auditoria</li>
                  <li>Revisão de permissões críticas</li>
                </ul>
              </div>
              <div className="admin-actions">
                <Button variant="outline" onClick={() => nav("/users")}>
                  Ir para Usuários
                </Button>
                <Button variant="ghost" onClick={() => nav("/permissions")}>
                  Abrir Matriz de Permissões
                </Button>
              </div>
            </div>
            <pre className="code-block">{JSON.stringify(result, null, 2)}</pre>
          </div>
        )}
      </Card>
    </div>
  );
}
