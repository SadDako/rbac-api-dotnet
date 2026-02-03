import React, { createContext, useContext, useMemo, useState } from "react";
import { logActivity } from "../activity";

type Me = {
  token: string;
  email: string;
  name: string;
  roles: string[];
};

type AuthContextType = {
  token: string | null;
  me: Me | null;
  isAuthenticated: boolean;
  login: (data: Me) => void;
  logout: () => void;
};

const AuthContext = createContext<AuthContextType | null>(null);

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [token, setToken] = useState<string | null>(localStorage.getItem("token"));
  const [me, setMe] = useState<Me | null>(() => {
    const raw = localStorage.getItem("me");
    return raw ? (JSON.parse(raw) as Me) : null;
  });

  function login(data: Me) {
    localStorage.setItem("token", data.token);
    localStorage.setItem("me", JSON.stringify(data));
    setToken(data.token);
    setMe(data);
    logActivity({
      type: "auth",
      status: "success",
      label: `Login realizado`,
      description: `Bem-vindo, ${data.name}.`,
    });
  }

  function logout() {
    localStorage.removeItem("token");
    localStorage.removeItem("me");
    setToken(null);
    setMe(null);
    logActivity({
      type: "auth",
      status: "success",
      label: "Logout realizado",
      description: "Sessão encerrada pelo usuário.",
    });
  }

  const value = useMemo(
    () => ({
      token,
      me,
      isAuthenticated: !!token,
      login,
      logout,
    }),
    [token, me]
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth() {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error("useAuth must be used within AuthProvider");
  return ctx;
}
