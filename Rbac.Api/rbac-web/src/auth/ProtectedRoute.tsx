import { Navigate, Outlet } from "react-router-dom";
import { logActivity } from "../activity";
import { useAuth } from "../auth/AuthContext";

export default function ProtectedRoute() {
  const { isAuthenticated } = useAuth();
  if (!isAuthenticated) {
    logActivity({
      type: "auth",
      status: "error",
      label: "Acesso negado",
      description: "Usuário não autenticado tentou acessar rota protegida.",
    });
    return <Navigate to="/login" replace />;
  }
  return <Outlet />;
}
