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

const PAGE_SIZE = 6;

export default function Roles() {
  const [roles, setRoles] = useState<RoleItem[]>([]);
  const [name, setName] = useState("");
  const [search, setSearch] = useState("");
  const [page, setPage] = useState(1);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [actionError, setActionError] = useState<string | null>(null);

  async function loadRoles() {
    setLoading(true);
    setError(null);
    try {
      const response = await apiFetch("/roles");
      const data = (await response.json()) as RoleItem[];
      setRoles(data);
    } catch (err: unknown) {
      if (err instanceof ApiError) {
        setError(err.message);
      } else if (err instanceof Error) {
        setError(err.message);
      } else {
        setError("Could not load roles.");
      }
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    loadRoles();
  }, []);

  useEffect(() => {
    setPage(1);
  }, [search]);

  const filtered = useMemo(() => {
    const term = search.trim().toLowerCase();
    if (!term) return roles;
    return roles.filter((role) => {
      return (
        role.name.toLowerCase().includes(term) ||
        role.permissions.some((permission) => permission.toLowerCase().includes(term))
      );
    });
  }, [roles, search]);

  const totalPages = Math.max(1, Math.ceil(filtered.length / PAGE_SIZE));
  const safePage = Math.min(page, totalPages);
  const pagedRoles = useMemo(() => {
    const start = (safePage - 1) * PAGE_SIZE;
    return filtered.slice(start, start + PAGE_SIZE);
  }, [filtered, safePage]);

  async function createRole() {
    if (!name.trim()) return;
    setActionError(null);
    try {
      await apiFetch("/roles", { method: "POST", body: JSON.stringify({ name }) });
      setName("");
      await loadRoles();
    } catch (err: unknown) {
      if (err instanceof ApiError) {
        setActionError(err.message);
      } else if (err instanceof Error) {
        setActionError(err.message);
      } else {
        setActionError("Could not create role.");
      }
    }
  }

  async function updateRole(roleId: string, newName: string) {
    if (!newName.trim()) return;
    setActionError(null);
    try {
      await apiFetch(`/roles/${roleId}`, { method: "PUT", body: JSON.stringify({ name: newName }) });
      await loadRoles();
    } catch (err: unknown) {
      if (err instanceof ApiError) {
        setActionError(err.message);
      } else if (err instanceof Error) {
        setActionError(err.message);
      } else {
        setActionError("Could not update role.");
      }
    }
  }

  async function deleteRole(roleId: string, roleName: string) {
    const confirmDelete = window.confirm(`Delete role "${roleName}"? This action cannot be undone.`);
    if (!confirmDelete) return;
    setActionError(null);
    try {
      await apiFetch(`/roles/${roleId}`, { method: "DELETE" });
      await loadRoles();
    } catch (err: unknown) {
      if (err instanceof ApiError) {
        setActionError(err.message);
      } else if (err instanceof Error) {
        setActionError(err.message);
      } else {
        setActionError("Could not remove role.");
      }
    }
  }

  return (
    <div className="roles-page">
      <Card title="Role management" description="Create, rename and remove roles with safer UX.">
        <div className="table-actions">
          <Input
            label="New role"
            placeholder="Example: Manager"
            value={name}
            onChange={(event) => setName(event.target.value)}
          />
          <Button variant="outline" onClick={createRole}>
            Create role
          </Button>
        </div>

        <div className="table-actions">
          <Input
            label="Search roles"
            placeholder="Role name or permission..."
            value={search}
            onChange={(event) => setSearch(event.target.value)}
          />
          <Button variant="ghost" onClick={loadRoles} disabled={loading}>
            Refresh
          </Button>
        </div>

        {actionError && (
          <Alert variant="warning" title="Attention">
            {actionError}
          </Alert>
        )}

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

        {!loading && !error && filtered.length === 0 && (
          <div className="empty-state">
            <strong>No roles found</strong>
            <p>Create a role or clear the search filter.</p>
          </div>
        )}

        {!loading && !error && filtered.length > 0 && (
          <div className="table-shell">
            <div className="table">
              <div className="table-row table-header">
                <span>Role</span>
                <span>Permissions</span>
                <span>Actions</span>
              </div>
              {pagedRoles.map((role) => (
                <div key={role.id} className="table-row">
                  <div className="inline-edit">
                    <input type="text" defaultValue={role.name} onBlur={(event) => updateRole(role.id, event.target.value)} />
                  </div>
                  <span className="muted">
                    {role.permissions.length > 0 ? role.permissions.join(", ") : "No permissions yet"}
                  </span>
                  <div className="table-actions-inline">
                    <Button variant="ghost" onClick={() => deleteRole(role.id, role.name)}>
                      Delete
                    </Button>
                  </div>
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
      </Card>
    </div>
  );
}
