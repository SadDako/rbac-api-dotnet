import { useEffect, useState } from "react";
import { apiFetch } from "../api";

export default function Dashboard({ onLogout }: { onLogout: () => void }) {
  const [me, setMe] = useState<any>(null);
  const [error, setError] = useState("");

  useEffect(() => {
    apiFetch("/test/me")
      .then((r) => r.json())
      .then(setMe)
      .catch((e) => setError(e.message));
  }, []);

  return (
    <div style={{ padding: 24, fontFamily: "system-ui" }}>
      <h1>Dashboard</h1>

      <button
        onClick={onLogout}
        style={{ padding: 10, marginBottom: 12 }}
      >
        Sair
      </button>

      {error && <p style={{ color: "crimson" }}>{error}</p>}
      {me && <pre>{JSON.stringify(me, null, 2)}</pre>}
    </div>
  );
}
