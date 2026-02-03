import { useEffect, useMemo, useState } from "react";
import { NavLink, Outlet, useLocation } from "react-router-dom";
import { logActivity } from "../activity";
import { useAuth } from "../auth/AuthContext";
import Button from "./Button";
import CommandPalette from "./CommandPalette";

type NavItem = {
  label: string;
  to: string;
  requiresAdmin?: boolean;
};

type NavGroup = {
  title: string;
  links: NavItem[];
};

const pageTitles: Record<string, string> = {
  "/": "Dashboard",
  "/admin": "Admin",
  "/playground": "RBAC Playground",
  "/account": "My Account",
  "/users": "Users",
  "/roles": "Roles",
  "/permissions": "Permissions Matrix",
  "/access-denied": "Access Denied",
};

export default function Layout() {
  const { me, logout } = useAuth();
  const location = useLocation();
  const [paletteOpen, setPaletteOpen] = useState(false);
  const roleText = me?.roles?.length ? me.roles.join(" · ") : "No roles";
  const initials = me?.name?.charAt(0) ?? "U";
  const isAdmin = useMemo(
    () => (me?.roles ?? []).some((role) => role.toLowerCase() === "admin"),
    [me?.roles]
  );

  const navGroups: NavGroup[] = [
    {
      title: "Main",
      links: [
        { label: "Dashboard", to: "/" },
        { label: "Playground", to: "/playground" },
        { label: "My Account", to: "/account" },
      ],
    },
    {
      title: "Administration",
      links: [
        { label: "Admin", to: "/admin", requiresAdmin: true },
        { label: "Users", to: "/users", requiresAdmin: true },
        { label: "Roles", to: "/roles", requiresAdmin: true },
        { label: "Permissions", to: "/permissions", requiresAdmin: true },
      ],
    },
  ];

  const pageTitle =
    pageTitles[location.pathname] ||
    Object.entries(pageTitles).find(([path]) => location.pathname.startsWith(path))?.[1] ||
    "RBAC Web";

  useEffect(() => {
    logActivity({
      type: "nav",
      status: "success",
      label: `Navigation: ${pageTitle}`,
      description: `Visited ${location.pathname}.`,
    });
  }, [location.pathname, pageTitle]);

  useEffect(() => {
    function handleKeydown(event: KeyboardEvent) {
      if ((event.ctrlKey || event.metaKey) && event.key.toLowerCase() === "k") {
        event.preventDefault();
        setPaletteOpen((prev) => !prev);
      }
    }

    window.addEventListener("keydown", handleKeydown);
    return () => window.removeEventListener("keydown", handleKeydown);
  }, []);

  return (
    <div className="app-shell">
      <aside className="sidebar">
        <div className="sidebar-brand">
          <div className="logo" />
          <div>
            <span>RBAC Web</span>
            <small>Access control platform</small>
          </div>
        </div>

        <nav className="sidebar-nav">
          {navGroups.map((group) => (
            <div key={group.title} className="nav-group">
              <p className="nav-group__title">{group.title}</p>
              {group.links.map((link) => {
                const disabled = link.requiresAdmin && !isAdmin;
                return (
                  <NavLink
                    key={link.to}
                    to={disabled ? "#" : link.to}
                    className={({ isActive }) =>
                      isActive ? "nav-link active" : disabled ? "nav-link disabled" : "nav-link"
                    }
                    onClick={(event) => {
                      if (disabled) event.preventDefault();
                    }}
                  >
                    {link.label}
                    {disabled && <span className="nav-link__badge">Admin</span>}
                  </NavLink>
                );
              })}
            </div>
          ))}
        </nav>

        <div className="sidebar-footer">
          <div className="user-summary">
            <div className="avatar" aria-hidden="true">
              {initials}
            </div>
            <div>
              <strong>{me?.name ?? "User"}</strong>
              <span>{me?.email ?? ""}</span>
            </div>
          </div>
          <div className="sidebar-status">
            <span className="status-dot" />
            <span>Session active</span>
          </div>
          <Button variant="ghost" onClick={logout} className="logout-btn">
            Sign out
          </Button>
        </div>
      </aside>

      <div className="app-main">
        <header className="topbar">
          <div>
            <p className="eyebrow">Welcome</p>
            <h1>{pageTitle}</h1>
          </div>
          <div className="topbar-actions">
            <Button variant="ghost" onClick={() => setPaletteOpen(true)} className="command-button">
              <span>Command Palette</span>
              <kbd>Ctrl + K</kbd>
            </Button>
            <div className="user-pill">
              <span>{me?.name ?? "User"}</span>
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

      <CommandPalette open={paletteOpen} onClose={() => setPaletteOpen(false)} />
    </div>
  );
}
