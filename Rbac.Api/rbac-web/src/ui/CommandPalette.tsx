import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { useNavigate } from "react-router-dom";
import { useAuth } from "../auth/AuthContext";

export type CommandPaletteProps = {
  open: boolean;
  onClose: () => void;
};

type CommandAction = {
  id: string;
  label: string;
  description: string;
  to?: string;
  keywords?: string;
  requiresAdmin?: boolean;
  action?: () => void;
};

const LISTBOX_ID = "command-palette-listbox";

export default function CommandPalette({ open, onClose }: CommandPaletteProps) {
  const { me, logout } = useAuth();
  const navigate = useNavigate();
  const [query, setQuery] = useState("");
  const [activeIndex, setActiveIndex] = useState(0);
  const inputRef = useRef<HTMLInputElement | null>(null);
  const lastFocusedElementRef = useRef<HTMLElement | null>(null);

  const isAdmin = useMemo(
    () => (me?.roles ?? []).some((role) => role.toLowerCase() === "admin"),
    [me?.roles]
  );

  const actions = useMemo<CommandAction[]>(
    () => [
      {
        id: "dashboard",
        label: "Dashboard",
        description: "Overview and activity",
        to: "/",
        keywords: "home overview",
      },
      {
        id: "admin",
        label: "Admin",
        description: "Administrative panel",
        to: "/admin",
        keywords: "admin panel",
        requiresAdmin: true,
      },
      {
        id: "playground",
        label: "Playground",
        description: "Test endpoints and token",
        to: "/playground",
        keywords: "rbac api",
      },
      {
        id: "account",
        label: "My Account",
        description: "Session and profile details",
        to: "/account",
        keywords: "profile me",
      },
      {
        id: "users",
        label: "Users",
        description: "User management",
        to: "/users",
        keywords: "users management",
        requiresAdmin: true,
      },
      {
        id: "roles",
        label: "Roles",
        description: "Role management",
        to: "/roles",
        keywords: "roles permissions",
        requiresAdmin: true,
      },
      {
        id: "permissions",
        label: "Permissions",
        description: "Permissions matrix",
        to: "/permissions",
        keywords: "permission matrix",
        requiresAdmin: true,
      },
      {
        id: "logout",
        label: "Logout",
        description: "End current session",
        keywords: "signout exit",
        action: logout,
      },
    ],
    [logout]
  );

  const filtered = useMemo(() => {
    const term = query.trim().toLowerCase();
    if (!term) return actions;
    return actions.filter((action) => {
      const target = `${action.label} ${action.description} ${action.keywords || ""}`.toLowerCase();
      return target.includes(term);
    });
  }, [actions, query]);

  const closePalette = useCallback(() => {
    setQuery("");
    setActiveIndex(0);
    lastFocusedElementRef.current?.focus();
    onClose();
  }, [onClose]);

  useEffect(() => {
    if (!open) {
      return;
    }

    lastFocusedElementRef.current = document.activeElement as HTMLElement;
    window.setTimeout(() => inputRef.current?.focus(), 0);
  }, [open]);

  useEffect(() => {
    function handleKeydown(event: KeyboardEvent) {
      if (!open) return;
      if (event.key === "Escape") {
        event.preventDefault();
        closePalette();
      }
      if (event.key === "ArrowDown") {
        event.preventDefault();
        setActiveIndex((prev) => (prev + 1) % Math.max(filtered.length, 1));
      }
      if (event.key === "ArrowUp") {
        event.preventDefault();
        setActiveIndex((prev) => (prev - 1 + Math.max(filtered.length, 1)) % Math.max(filtered.length, 1));
      }
      if (event.key === "Enter") {
        event.preventDefault();
        const action = filtered[activeIndex];
        if (!action) return;
        if (action.requiresAdmin && !isAdmin) return;
        if (action.action) action.action();
        if (action.to) navigate(action.to);
        closePalette();
      }
    }

    window.addEventListener("keydown", handleKeydown);
    return () => window.removeEventListener("keydown", handleKeydown);
  }, [open, filtered, activeIndex, isAdmin, navigate, closePalette]);

  if (!open) return null;

  return (
    <div className="palette-overlay" role="dialog" aria-modal="true" aria-label="Command palette">
      <div className="palette">
        <div className="palette-header">
          <input
            ref={inputRef}
            type="text"
            role="combobox"
            aria-expanded="true"
            aria-controls={LISTBOX_ID}
            aria-activedescendant={filtered[activeIndex]?.id}
            placeholder="Search commands, pages and shortcuts..."
            value={query}
            onChange={(event) => {
              setQuery(event.target.value);
              setActiveIndex(0);
            }}
          />
          <span className="palette-hint">ESC</span>
        </div>
        <div id={LISTBOX_ID} className="palette-body" role="listbox" aria-label="Command results">
          {filtered.length === 0 && (
            <div className="palette-empty">
              <strong>No command found.</strong>
              <span>Try keywords like admin, roles or logout.</span>
            </div>
          )}
          {filtered.map((action, index) => {
            const disabled = action.requiresAdmin && !isAdmin;
            return (
              <button
                key={action.id}
                id={action.id}
                role="option"
                aria-selected={index === activeIndex}
                type="button"
                className={`palette-item ${index === activeIndex ? "active" : ""} ${
                  disabled ? "disabled" : ""
                }`}
                onMouseEnter={() => setActiveIndex(index)}
                onClick={() => {
                  if (disabled) return;
                  if (action.action) action.action();
                  if (action.to) navigate(action.to);
                  closePalette();
                }}
              >
                <div>
                  <strong>{action.label}</strong>
                  <small>{action.description}</small>
                </div>
                {disabled ? <span className="badge badge--warning">Admin</span> : <span>↵</span>}
              </button>
            );
          })}
        </div>
      </div>
      <button className="palette-backdrop" aria-label="Close palette" onClick={closePalette} />
    </div>
  );
}
