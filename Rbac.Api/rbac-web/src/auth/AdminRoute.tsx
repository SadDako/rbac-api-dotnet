import { useEffect } from "react";
import { Navigate, Outlet } from "react-router-dom";
import { logActivity } from "../activity";
import { useAuth } from "./AuthContext";

export default function AdminRoute() {
  const { me } = useAuth();
  const isAdmin = (me?.roles ?? []).some((role) => role.toLowerCase() === "admin");

  useEffect(() => {
    if (!isAdmin) {
      logActivity({
        type: "auth",
        status: "error",
        label: "Admin access denied",
        description: "User without Admin role attempted to access admin routes.",
      });
    } else {
      logActivity({
        type: "auth",
        status: "success",
        label: "Admin access granted",
        description: "Admin role validated for restricted route.",
      });
    }
  }, [isAdmin]);

  if (!isAdmin) {
    return <Navigate to="/access-denied" replace />;
  }

  return <Outlet />;
}
