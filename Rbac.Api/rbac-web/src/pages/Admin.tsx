import { useEffect, useState } from "react";
import { apiFetch } from "../api";
import Card from "../ui/Card";

export default function Admin() {
  const [result, setResult] = useState<any>(null);
  const [error, setError] = useState("");
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    setLoading(true);
    setError("");
    apiFetch("/test/admin")
      .then((r) => r.json())
      .then(setResult)
      .catch((e: any) => setError(e?.message || "Erro ao acessar o admin"))
      .finally(() => setLoading(false));
  }, []);

  const isForbidden = error.toLowerCase().includes("403") || error.toLowerCase().includes("forbidden");

  return (
    <div className="admin">
      <Card title="Área Administrativa" description="Acesso restrito a usuários Admin.">
        {loading && <div className="alert">Carregando permissões...</div>}

        {!loading && error && isForbidden && (
          <div className="alert alert--warning">
            <strong>403 · Acesso negado</strong>
            <p>Seu usuário não possui permissão para visualizar este painel.</p>
          </div>
        )}

        {!loading && error && !isForbidden && (
          <div className="alert alert--error">{error}</div>
        )}

        {!loading && !error && (
          <pre className="code-block">{JSON.stringify(result, null, 2)}</pre>
        )}
      </Card>
    </div>
  );
}
