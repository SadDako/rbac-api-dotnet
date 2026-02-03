import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import ActivityFeed from "./ActivityFeed";

vi.mock("../activity", async () => {
  const actual = await vi.importActual<typeof import("../activity")>("../activity");
  return {
    ...actual,
    fetchRemoteActivity: vi.fn().mockResolvedValue([]),
  };
});

describe("ActivityFeed", () => {
  it("filters events by status", async () => {
    const events = [
      {
        id: "1",
        type: "api" as const,
        status: "success" as const,
        label: "Loaded users",
        at: new Date().toISOString(),
      },
      {
        id: "2",
        type: "api" as const,
        status: "error" as const,
        label: "Failed users",
        at: new Date().toISOString(),
      },
    ];

    render(<ActivityFeed events={events} />);
    const user = userEvent.setup();

    await waitFor(() => expect(screen.getByText("Loaded users")).toBeInTheDocument());
    await user.selectOptions(screen.getByLabelText("Status"), "error");

    expect(screen.queryByText("Loaded users")).not.toBeInTheDocument();
    expect(screen.getByText("Failed users")).toBeInTheDocument();
  });
});
