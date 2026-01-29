import { Link } from "react-router-dom";
import { useAuth } from "../auth/AuthContext";

export default function NotFound() {
  const { isAuthenticated } = useAuth();

  return (
    <div className="not-found">
      <div className="not-found__card">
        <div>
          <h1>404</h1>
          <p>Página não encontrada. Verifique o endereço informado.</p>
          <small className="muted">RBAC protege seus caminhos — volte para uma rota válida.</small>
        </div>
        <div className="not-found__actions">
          <Link className="btn btn--primary" to={isAuthenticated ? "/" : "/login"}>
            {isAuthenticated ? "Voltar ao Dashboard" : "Ir para Login"}
          </Link>
        </div>
      </div>
    </div>
  );
}
