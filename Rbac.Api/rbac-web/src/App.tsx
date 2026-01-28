import { useState } from "react";
import Login from "./pages/Login";
import Dashboard from "./pages/Dashboard";

export default function App() {
  const [logged, setLogged] = useState(!!localStorage.getItem("token"));

  function logout() {
    localStorage.removeItem("token");
    localStorage.removeItem("me");
    setLogged(false);
  }

  return logged ? (
    <Dashboard onLogout={logout} />
  ) : (
    <Login onLogged={() => setLogged(true)} />
  );
}
