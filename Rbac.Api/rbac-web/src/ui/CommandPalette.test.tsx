import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { useState } from "react";
import { MemoryRouter, Route, Routes, useLocation } from "react-router-dom";
import { beforeEach, describe, expect, it } from "vitest";
import { AuthProvider } from "../auth/AuthContext";
import CommandPalette from "./CommandPalette";

function LocationPreview() {
  const location = useLocation();
  return <div data-testid="location">{location.pathname}</div>;
}

function PaletteHarness() {
  const [open, setOpen] = useState(true);
  return (
    <>
      <CommandPalette open={open} onClose={() => setOpen(false)} />
      <LocationPreview />
    </>
  );
}

function renderPalette() {
  return render(
    <MemoryRouter initialEntries={["/"]}>
      <AuthProvider>
        <Routes>
          <Route path="*" element={<PaletteHarness />} />
        </Routes>
      </AuthProvider>
    </MemoryRouter>
  );
}

describe("CommandPalette", () => {
  beforeEach(() => {
    localStorage.clear();
    localStorage.setItem("token", "fake-token");
    localStorage.setItem(
      "me",
      JSON.stringify({
        token: "fake-token",
        name: "Admin",
        email: "admin@rbac.local",
        roles: ["Admin"],
      })
    );
  });

  it("closes with Escape key", async () => {
    const user = userEvent.setup();
    renderPalette();

    expect(screen.getByRole("dialog", { name: "Command palette" })).toBeInTheDocument();
    await user.keyboard("{Escape}");
    expect(screen.queryByRole("dialog", { name: "Command palette" })).not.toBeInTheDocument();
  });

  it("navigates with Enter on selected command", async () => {
    const user = userEvent.setup();
    renderPalette();

    const input = screen.getByRole("combobox");
    await user.type(input, "account");
    await user.keyboard("{Enter}");

    expect(screen.getByTestId("location")).toHaveTextContent("/account");
  });
});
