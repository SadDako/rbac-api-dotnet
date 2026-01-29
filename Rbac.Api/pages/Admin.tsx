import { useEffect, useState } from "react";
import { apiFetch } from "../api";

export default function Admin() {
  const [result, setResult] = useState<any>(null);
  const [error, setError] = useState("");

  useEffect(() => {
    apiFetch("/test/admin")
      .then((r) => r.json())
      .then(setResult)
      .catch((e) => setError(e.message));
  }, []);

  return (
    <div className="card">
      <h2>Admin</h2>
      {error ? <div className="alert">{error}</div> : <pre className="code">{JSON.stringify(result, null, 2)}</pre>}
    </div>
  );
}
