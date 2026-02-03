import { render, screen } from "@testing-library/react";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { beforeEach, describe, expect, it } from "vitest";
import { AuthProvider } from "./AuthContext";
import AdminRoute from "./AdminRoute";

function renderAdminRoute() {
  return render(
    <MemoryRouter initialEntries={["/admin"]}>
      <AuthProvider>
        <Routes>
          <Route path="/access-denied" element={<div>Access denied page</div>} />
          <Route element={<AdminRoute />}>
            <Route path="/admin" element={<div>Admin content</div>} />
          </Route>
        </Routes>
      </AuthProvider>
    </MemoryRouter>
  );
}

describe("AdminRoute", () => {
  beforeEach(() => {
    localStorage.clear();
    localStorage.setItem("token", "fake-token");
  });

  it("redirects non-admin users to access denied", () => {
    localStorage.setItem(
      "me",
      JSON.stringify({
        token: "fake-token",
        name: "User",
        email: "user@rbac.local",
        roles: ["User"],
      })
    );

    renderAdminRoute();
    expect(screen.getByText("Access denied page")).toBeInTheDocument();
  });

  it("renders admin content when Admin role exists", () => {
    localStorage.setItem(
      "me",
      JSON.stringify({
        token: "fake-token",
        name: "Admin",
        email: "admin@rbac.local",
        roles: ["Admin"],
      })
    );

    renderAdminRoute();
    expect(screen.getByText("Admin content")).toBeInTheDocument();
  });
});
