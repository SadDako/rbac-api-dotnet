import { useEffect } from "react";
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
  const roleText = me?.roles?.length ? me.roles.join(" · ") : "Sem roles";
  const initials = me?.name?.charAt(0) ?? "U";

  useEffect(() => {
    try {
      const stored = localStorage.getItem("rbac-activity");
      const history = stored ? (JSON.parse(stored) as Array<{ id: string; label: string; at: string }>) : [];
      const entry = {
        id: `${Date.now()}`,
        label: `Visitou ${getPageTitle(location.pathname)}`,
        at: new Date().toISOString(),
      };
      const next = [entry, ...history].slice(0, 12);
      localStorage.setItem("rbac-activity", JSON.stringify(next));
      window.dispatchEvent(new Event("rbac-activity"));
    } catch {
      // ignore storage errors
    }
  }, [location.pathname]);

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
              {initials}
            </div>
            <div>
              <strong>{me?.name ?? "Usuário"}</strong>
              <span>{me?.email ?? ""}</span>
            </div>
          </div>
          <div className="sidebar-status">
            <span className="status-dot" />
            <span>Sessão ativa</span>
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
              <small>{roleText}</small>
            </div>
            <Button variant="outline" onClick={logout}>
              Logout
            </Button>
          </div>
        </header>

        <main className="content">
          <div className="route-frame" key={location.pathname}>
            <Outlet />
          </div>
        </main>
      </div>
    </div>
  );
}
