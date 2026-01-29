import { useEffect, useState } from "react";
import { apiFetch } from "../api";

export default function Dashboard() {
  const [me, setMe] = useState<any>(null);

  useEffect(() => {
    apiFetch("/test/me")
      .then((r) => r.json())
      .then(setMe)
      .catch(() => setMe(null));
  }, []);

  return (
    <div className="card">
      <h2>Dashboard</h2>
      <p>Status: autenticado</p>
      <pre className="code">{me ? JSON.stringify(me, null, 2) : "Carregando"}</pre>
    </div>
  );
}
