import { useEffect, useMemo, useState } from "react";
import { ApiError, apiFetch } from "../api";
import Alert from "../ui/Alert";
import Badge from "../ui/Badge";
import Button from "../ui/Button";
import Card from "../ui/Card";
import Input from "../ui/Input";
import Skeleton from "../ui/Skeleton";

type UserItem = {
  id: string;
  email: string;
  name: string;
  roles: string[];
};

type RoleItem = {
  id: string;
  name: string;
};

const PAGE_SIZE = 6;

export default function Users() {
  const [users, setUsers] = useState<UserItem[]>([]);
  const [roles, setRoles] = useState<RoleItem[]>([]);
  const [search, setSearch] = useState("");
  const [page, setPage] = useState(1);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [actionError, setActionError] = useState<string | null>(null);

  async function loadData() {
    setLoading(true);
    setError(null);
    try {
      const [usersRes, rolesRes] = await Promise.all([apiFetch("/users"), apiFetch("/roles")]);
      const usersData = (await usersRes.json()) as UserItem[];
      const rolesData = (await rolesRes.json()) as RoleItem[];
      setUsers(usersData);
      setRoles(rolesData);
    } catch (err: unknown) {
      if (err instanceof ApiError) {
        setError(err.message);
      } else if (err instanceof Error) {
        setError(err.message);
      } else {
        setError("Could not load users.");
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

  const filtered = useMemo(() => {
    const term = search.trim().toLowerCase();
    if (!term) return users;
    return users.filter((user) => {
      return (
        user.name.toLowerCase().includes(term) ||
        user.email.toLowerCase().includes(term) ||
        user.roles.some((role) => role.toLowerCase().includes(term))
      );
    });
  }, [search, users]);

  const totalPages = Math.max(1, Math.ceil(filtered.length / PAGE_SIZE));
  const safePage = Math.min(page, totalPages);

  const pagedUsers = useMemo(() => {
    const start = (safePage - 1) * PAGE_SIZE;
    return filtered.slice(start, start + PAGE_SIZE);
  }, [filtered, safePage]);

  async function assignRole(userId: string, roleId: string) {
    if (!roleId) return;
    setActionError(null);
    try {
      await apiFetch(`/users/${userId}/roles`, {
        method: "POST",
        body: JSON.stringify({ roleId }),
      });
      await loadData();
    } catch (err: unknown) {
      if (err instanceof ApiError) {
        setActionError(err.message);
      } else if (err instanceof Error) {
        setActionError(err.message);
      } else {
        setActionError("Could not update roles.");
      }
    }
  }

  async function removeRole(userId: string, roleId: string, roleName: string) {
    const confirmed = window.confirm(`Remove role "${roleName}" from this user?`);
    if (!confirmed) return;

    setActionError(null);
    try {
      await apiFetch(`/users/${userId}/roles/${roleId}`, { method: "DELETE" });
      await loadData();
    } catch (err: unknown) {
      if (err instanceof ApiError) {
        setActionError(err.message);
      } else if (err instanceof Error) {
        setActionError(err.message);
      } else {
        setActionError("Could not update roles.");
      }
    }
  }

  return (
    <div className="users-page">
      <Card title="User management" description="Search users, inspect roles and update assignments safely.">
        <div className="table-actions">
          <Input
            label="Search users"
            placeholder="Name, email or role..."
            value={search}
            onChange={(event) => setSearch(event.target.value)}
          />
          <Button variant="outline" onClick={loadData} disabled={loading}>
            Refresh
          </Button>
        </div>

        <p className="muted">
          Showing {pagedUsers.length} of {filtered.length} user(s).
        </p>

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
            <strong>No users found</strong>
            <p>Try a different search term or refresh the list.</p>
          </div>
        )}

        {!loading && !error && filtered.length > 0 && (
          <div className="table-shell">
            <div className="table">
              <div className="table-row table-header">
                <span>User</span>
                <span>Email</span>
                <span>Roles</span>
                <span>Actions</span>
              </div>
              {pagedUsers.map((user) => (
                <div key={user.id} className="table-row">
                  <strong>{user.name}</strong>
                  <span>{user.email}</span>
                  <div className="chip-list">
                    {user.roles.length === 0 && <Badge variant="default">No roles</Badge>}
                    {user.roles.map((roleName) => {
                      const roleId = roles.find((role) => role.name === roleName)?.id;
                      return (
                        <button
                          key={roleName}
                          className="chip"
                          onClick={() => {
                            if (!roleId) return;
                            removeRole(user.id, roleId, roleName);
                          }}
                        >
                          {roleName}
                          <span>×</span>
                        </button>
                      );
                    })}
                  </div>
                  <div className="table-actions-inline">
                    <select defaultValue="" onChange={(event) => assignRole(user.id, event.target.value)}>
                      <option value="">Add role</option>
                      {roles.map((role) => (
                        <option key={role.id} value={role.id}>
                          {role.name}
                        </option>
                      ))}
                    </select>
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
