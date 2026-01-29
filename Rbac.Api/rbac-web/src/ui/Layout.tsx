import { NavLink, Outlet, useLocation } from "react-router-dom";
import { useAuth } from "../auth/AuthContext";
import Button from "./Button";

const navLinks = [
  { label: "Dashboard", to: "/" },
  { label: "Admin", to: "/admin" },
];

function getPageTitle(pathname: string) {
  if (pathname.startsWith("/admin")) return "Admin";
  if (pathname === "/") return "Dashboard";
  return "RBAC Web";
}

export default function Layout() {
  const { me, logout } = useAuth();
  const location = useLocation();

  return (
    <div className="app-shell">
      <aside className="sidebar">
        <div className="sidebar-brand">
          <div className="logo" />
          <div>
            <span>RBAC Web</span>
            <small>Controle de acesso</small>
          </div>
        </div>

        <nav className="sidebar-nav">
          {navLinks.map((link) => (
            <NavLink
              key={link.to}
              to={link.to}
              className={({ isActive }) => (isActive ? "nav-link active" : "nav-link")}
            >
              {link.label}
            </NavLink>
          ))}
        </nav>

        <div className="sidebar-footer">
          <div className="user-summary">
            <div className="avatar" aria-hidden="true">
              {me?.name?.charAt(0) ?? "U"}
            </div>
            <div>
              <strong>{me?.name ?? "Usuário"}</strong>
              <span>{me?.email ?? ""}</span>
            </div>
          </div>
          <Button variant="ghost" onClick={logout} className="logout-btn">
            Sair
          </Button>
        </div>
      </aside>

      <div className="app-main">
        <header className="topbar">
          <div>
            <p className="eyebrow">Bem-vindo</p>
            <h1>{getPageTitle(location.pathname)}</h1>
          </div>
          <div className="topbar-actions">
            <div className="user-pill">
              <span>{me?.name ?? "Usuário"}</span>
              <small>{me?.roles?.join(" · ") ?? ""}</small>
            </div>
            <Button variant="outline" onClick={logout}>
              Logout
            </Button>
          </div>
        </header>

        <main className="content">
          <Outlet />
        </main>
      </div>
    </div>
  );
}
