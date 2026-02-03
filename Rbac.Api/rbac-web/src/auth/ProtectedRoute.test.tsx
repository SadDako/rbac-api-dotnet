import { render, screen } from "@testing-library/react";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { beforeEach, describe, expect, it } from "vitest";
import { AuthProvider } from "./AuthContext";
import ProtectedRoute from "./ProtectedRoute";

function renderWithRouter() {
  return render(
    <MemoryRouter initialEntries={["/"]}>
      <AuthProvider>
        <Routes>
          <Route element={<ProtectedRoute />}>
            <Route path="/" element={<div>Private dashboard</div>} />
          </Route>
          <Route path="/login" element={<div>Login page</div>} />
        </Routes>
      </AuthProvider>
    </MemoryRouter>
  );
}

describe("ProtectedRoute", () => {
  beforeEach(() => {
    localStorage.clear();
  });

  it("redirects unauthenticated users to login", () => {
    renderWithRouter();
    expect(screen.getByText("Login page")).toBeInTheDocument();
  });

  it("renders children when token exists", () => {
    localStorage.setItem("token", "fake-token");
    localStorage.setItem(
      "me",
      JSON.stringify({
        token: "fake-token",
        name: "User",
        email: "user@rbac.local",
        roles: ["User"],
      })
    );

    renderWithRouter();
    expect(screen.getByText("Private dashboard")).toBeInTheDocument();
  });
});
