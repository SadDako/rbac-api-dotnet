import { useMemo, useState } from "react";
import { ApiError, apiFetch } from "../api";
import { useAuth } from "../auth/AuthContext";
import Alert from "../ui/Alert";
import Button from "../ui/Button";
import Card from "../ui/Card";

const endpoints = [
  { label: "/users/me", path: "/users/me", method: "GET" },
  { label: "/admin/ping", path: "/admin/ping", method: "GET" },
  { label: "/admin/whoami", path: "/admin/whoami", method: "GET" },
  { label: "/permissions", path: "/permissions", method: "GET" },
];

type ResultState = {
  status?: number;
  json?: unknown;
  error?: string;
};

type DecodedToken = {
  exp?: number;
  role?: string | string[];
  roles?: string[];
  permissions?: string[];
};

function decodeToken(token?: string | null) {
  if (!token) return null;
  const parts = token.split(".");
  if (parts.length < 2) return null;
  try {
    return JSON.parse(atob(parts[1].replace(/-/g, "+").replace(/_/g, "/"))) as DecodedToken;
  } catch {
    return null;
  }
}

export default function Playground() {
  const { token } = useAuth();
  const [result, setResult] = useState<ResultState | null>(null);
  const [loading, setLoading] = useState(false);

  const decoded = useMemo(() => decodeToken(token), [token]);
  const exp = decoded?.exp ? new Date(decoded.exp * 1000) : null;
  const remainingMs = exp ? exp.getTime() - Date.now() : null;
  const remaining =
    remainingMs === null
      ? "-"
      : remainingMs <= 0
        ? "Expirado"
        : `${Math.floor(remainingMs / 60000)} min`;

  async function handleTest(endpoint: (typeof endpoints)[number]) {
    setLoading(true);
    setResult(null);
    try {
      const response = await apiFetch(endpoint.path, { method: endpoint.method });
      const data = await response.json();
      setResult({ status: response.status, json: data });
    } catch (err: unknown) {
      if (err instanceof ApiError) {
        const friendly = err.code === "FORBIDDEN" ? "Acesso negado (403)." : err.message;
        setResult({ status: err.status, error: friendly, json: err.details });
      } else if (err instanceof Error) {
        setResult({ error: err.message });
      } else {
        setResult({ error: "Falha ao testar o endpoint." });
      }
    } finally {
      setLoading(false);
    }
  }

  function copyCurl(endpoint: (typeof endpoints)[number]) {
    const curl = `curl -H "Authorization: Bearer ${token}" ${
      endpoint.method === "GET" ? "" : `-X ${endpoint.method} `
    }http://localhost:5083${endpoint.path}`.trim();
    navigator.clipboard.writeText(curl);
  }

  return (
    <div className="playground">
      <section className="grid-2">
        <Card title="RBAC Playground" description="Teste endpoints protegidos e visualize respostas.">
          <div className="playground-actions">
            {endpoints.map((endpoint) => (
              <div key={endpoint.path} className="playground-action">
                <div>
                  <strong>{endpoint.label}</strong>
                  <span>{endpoint.method}</span>
                </div>
                <div className="playground-buttons">
                  <Button variant="outline" onClick={() => handleTest(endpoint)} disabled={loading}>
                    Testar endpoint
                  </Button>
                  <Button variant="ghost" onClick={() => copyCurl(endpoint)} disabled={!token}>
                    Copiar curl
                  </Button>
                </div>
              </div>
            ))}
          </div>
          {loading && <p className="muted">Executando requisição...</p>}
        </Card>

        <Card title="Resultado" description="Status HTTP, payload e mensagens amigáveis.">
          {!result && <p className="muted">Selecione um endpoint para iniciar o teste.</p>}
          {result?.error && (
            <Alert variant={result.status === 401 ? "warning" : "error"} title="Erro">
              {result.error}
            </Alert>
          )}
          {result?.status && (
            <div className="status-pill">
              Status HTTP: <strong>{result.status}</strong>
            </div>
          )}
          {result?.json !== undefined && result?.json !== null && (
            <pre className="code-block">{JSON.stringify(result.json, null, 2)}</pre>
          )}
        </Card>
      </section>

      <section className="grid-2">
        <Card title="Token decodificado" description="Claims principais e expiração.">
          {decoded ? (
            <div className="token-grid">
              <div>
                <span>exp</span>
                <strong>{exp ? exp.toLocaleString() : "-"}</strong>
                <small>Tempo restante: {remaining}</small>
              </div>
              <div>
                <span>roles</span>
                <strong>{(decoded?.role || decoded?.roles || []).toString() || "-"}</strong>
                <small>Derivado das claims do JWT.</small>
              </div>
              <div>
                <span>permissions</span>
                <strong>{(decoded?.permissions || []).toString() || "-"}</strong>
                <small>Quando disponível no token.</small>
              </div>
            </div>
          ) : (
            <p className="muted">Token não encontrado.</p>
          )}
        </Card>

        <Card title="Boas práticas" description="Como interpretar as respostas">
          <ul className="bullet-list">
            <li>401 indica token inválido ou expirado.</li>
            <li>403 indica falta de permissão para o recurso.</li>
            <li>Use o curl para reproduzir a chamada fora do UI.</li>
          </ul>
        </Card>
      </section>
    </div>
  );
}
