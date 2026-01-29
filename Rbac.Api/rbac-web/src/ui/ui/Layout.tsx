import { Link, Outlet } from "react-router-dom";
import { useAuth } from "../auth/AuthContext";

export default function Layout() {
  const { me, logout } = useAuth();

  return (
    <div className="app">
      <header className="topbar">
        <div className="brand">
          <div className="logo" />
          <span>RBAC Web</span>
        </div>

        <nav className="nav">
          <Link to="/">Dashboard</Link>
          <Link to="/admin">Admin</Link>
        </nav>

        <div className="userbox">
          <div className="usertext">
            <div className="name">{me?.name ?? "Usuário"}</div>
            <div className="meta">{me?.roles?.join(", ") ?? ""}</div>
          </div>
          <button className="btn ghost" onClick={logout}>
            Sair
          </button>
        </div>
      </header>

      <main className="container">
        <Outlet />
      </main>
    </div>
  );
}
