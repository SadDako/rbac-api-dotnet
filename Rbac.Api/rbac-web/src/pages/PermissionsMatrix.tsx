import { useEffect, useMemo, useState } from "react";
import { ApiError, apiFetch } from "../api";
import Alert from "../ui/Alert";
import Button from "../ui/Button";
import Card from "../ui/Card";
import Input from "../ui/Input";
import Skeleton from "../ui/Skeleton";

type RoleItem = {
  id: string;
  name: string;
  permissions: string[];
};

type PermissionItem = {
  id: string;
  key: string;
  description: string;
};

const PAGE_SIZE = 12;

export default function PermissionsMatrix() {
  const [roles, setRoles] = useState<RoleItem[]>([]);
  const [permissions, setPermissions] = useState<PermissionItem[]>([]);
  const [search, setSearch] = useState("");
  const [page, setPage] = useState(1);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);
  const [changes, setChanges] = useState<Record<string, Set<string>>>({});
  const [message, setMessage] = useState<string | null>(null);

  async function loadData() {
    setLoading(true);
    setError(null);
    try {
      const [rolesRes, permissionsRes] = await Promise.all([apiFetch("/roles"), apiFetch("/permissions")]);
      const roleData = (await rolesRes.json()) as RoleItem[];
      const permissionData = (await permissionsRes.json()) as PermissionItem[];
      setRoles(roleData);
      setPermissions(permissionData);
      const initial: Record<string, Set<string>> = {};
      roleData.forEach((role) => {
        initial[role.id] = new Set(role.permissions || []);
      });
      setChanges(initial);
    } catch (err: unknown) {
      if (err instanceof ApiError) {
        setError(err.message);
      } else if (err instanceof Error) {
        setError(err.message);
      } else {
        setError("Could not load permissions.");
      }
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    loadData();
  }, []);

  useEffect(() => {
    setPage(1);
  }, [search]);

  const filteredPermissions = useMemo(() => {
    const term = search.trim().toLowerCase();
    if (!term) return permissions;
    return permissions.filter((permission) => {
      return (
        permission.key.toLowerCase().includes(term) ||
        permission.description.toLowerCase().includes(term)
      );
    });
  }, [permissions, search]);

  const totalPages = Math.max(1, Math.ceil(filteredPermissions.length / PAGE_SIZE));
  const safePage = Math.min(page, totalPages);
  const visiblePermissions = useMemo(() => {
    const start = (safePage - 1) * PAGE_SIZE;
    return filteredPermissions.slice(start, start + PAGE_SIZE);
  }, [filteredPermissions, safePage]);

  const hasPending = useMemo(() => {
    return roles.some((role) => {
      const current = new Set(role.permissions || []);
      const updated = changes[role.id];
      if (!updated) return false;
      if (current.size !== updated.size) return true;
      for (const perm of current) {
        if (!updated.has(perm)) return true;
      }
      return false;
    });
  }, [roles, changes]);

  function togglePermission(roleId: string, permissionKey: string) {
    setChanges((prev) => {
      const next = { ...prev };
      const set = new Set(next[roleId] ?? []);
      if (set.has(permissionKey)) {
        set.delete(permissionKey);
      } else {
        set.add(permissionKey);
      }
      next[roleId] = set;
      return next;
    });
  }

  async function saveChanges() {
    setSaving(true);
    setMessage(null);
    setError(null);
    try {
      await Promise.all(
        roles.map((role) =>
          apiFetch(`/roles/${role.id}/permissions`, {
            method: "PUT",
            body: JSON.stringify({ permissions: Array.from(changes[role.id] || []) }),
          })
        )
      );
      setMessage("Permissions updated successfully.");
      await loadData();
    } catch (err: unknown) {
      if (err instanceof ApiError) {
        setError(err.message);
      } else if (err instanceof Error) {
        setError(err.message);
      } else {
        setError("Could not save permissions.");
      }
    } finally {
      setSaving(false);
    }
  }

  return (
    <div className="permissions-page">
      <Card title="Permissions matrix" description="Map permissions to roles with pending-change indicators.">
        <div className="table-actions">
          <Input
            label="Search permissions"
            placeholder="Permission key or description..."
            value={search}
            onChange={(event) => setSearch(event.target.value)}
          />
          <Button variant="ghost" onClick={loadData} disabled={loading}>
            Refresh
          </Button>
        </div>

        {loading && (
          <div className="skeleton-stack">
            <Skeleton className="skeleton-line" />
            <Skeleton className="skeleton-line" />
            <Skeleton className="skeleton-line skeleton-line--short" />
          </div>
        )}

        {!loading && error && (
          <Alert variant="error" title="Error">
            {error}
          </Alert>
        )}

        {message && (
          <Alert variant="success" title="Saved">
            {message}
          </Alert>
        )}

        {!loading && !error && filteredPermissions.length === 0 && (
          <div className="empty-state">
            <strong>No permissions found</strong>
            <p>Try another search term.</p>
          </div>
        )}

        {!loading && !error && filteredPermissions.length > 0 && (
          <div className="table-shell">
            <div className="matrix">
              <div className="matrix-row matrix-header">
                <span>Permission</span>
                {roles.map((role) => (
                  <span key={role.id}>{role.name}</span>
                ))}
              </div>
              {visiblePermissions.map((permission) => (
                <div key={permission.id} className="matrix-row">
                  <div>
                    <strong>{permission.key}</strong>
                    <small>{permission.description}</small>
                  </div>
                  {roles.map((role) => {
                    const assigned = changes[role.id]?.has(permission.key);
                    const initial = role.permissions?.includes(permission.key);
                    const pending = assigned !== initial;
                    return (
                      <label key={`${role.id}-${permission.id}`} className={`matrix-cell ${pending ? "pending" : ""}`}>
                        <input type="checkbox" checked={!!assigned} onChange={() => togglePermission(role.id, permission.key)} />
                        <span />
                      </label>
                    );
                  })}
                </div>
              ))}
            </div>

            <div className="table-footer">
              <Button variant="ghost" disabled={safePage <= 1} onClick={() => setPage((current) => current - 1)}>
                Previous
              </Button>
              <span className="muted">
                Page {safePage} of {totalPages}
              </span>
              <Button
                variant="ghost"
                disabled={safePage >= totalPages}
                onClick={() => setPage((current) => current + 1)}
              >
                Next
              </Button>
            </div>
          </div>
        )}

        <div className="matrix-footer">
          <div>
            {hasPending ? (
              <span className="muted">There are unsaved permission changes.</span>
            ) : (
              <span className="muted">No pending changes.</span>
            )}
          </div>
          <Button variant="outline" onClick={saveChanges} disabled={!hasPending || saving}>
            {saving ? "Saving..." : "Save changes"}
          </Button>
        </div>
      </Card>
    </div>
  );
}
