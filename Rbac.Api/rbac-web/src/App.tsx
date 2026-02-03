import { Suspense, lazy } from "react";
import { BrowserRouter, Navigate, Route, Routes } from "react-router-dom";
import { AuthProvider } from "./auth/AuthContext";
import AdminRoute from "./auth/AdminRoute";
import ProtectedRoute from "./auth/ProtectedRoute";
import Spinner from "./ui/Spinner";
import ApiNoticeHost from "./ui/ApiNoticeHost";

const Layout = lazy(() => import("./ui/Layout"));
const Dashboard = lazy(() => import("./pages/Dashboard"));
const Login = lazy(() => import("./pages/Login"));
const Admin = lazy(() => import("./pages/Admin"));
const NotFound = lazy(() => import("./pages/NotFound"));
const Playground = lazy(() => import("./pages/Playground"));
const Account = lazy(() => import("./pages/Account"));
const Users = lazy(() => import("./pages/Users"));
const Roles = lazy(() => import("./pages/Roles"));
const PermissionsMatrix = lazy(() => import("./pages/PermissionsMatrix"));
const AccessDenied = lazy(() => import("./pages/AccessDenied"));

function RouteFallback() {
  return (
    <div className="route-loading" role="status" aria-live="polite">
      <Spinner />
      <span>Loading page...</span>
    </div>
  );
}

export default function App() {
  return (
    <BrowserRouter>
      <AuthProvider>
        <ApiNoticeHost />
        <Suspense fallback={<RouteFallback />}>
          <Routes>
            <Route path="/login" element={<Login />} />
            <Route element={<ProtectedRoute />}>
              <Route element={<Layout />}>
                <Route path="/" element={<Dashboard />} />
                <Route path="/playground" element={<Playground />} />
                <Route path="/account" element={<Account />} />
                <Route path="/access-denied" element={<AccessDenied />} />
                <Route element={<AdminRoute />}>
                  <Route path="/admin" element={<Admin />} />
                  <Route path="/users" element={<Users />} />
                  <Route path="/roles" element={<Roles />} />
                  <Route path="/permissions" element={<PermissionsMatrix />} />
                </Route>
              </Route>
            </Route>
            <Route path="/not-found" element={<NotFound />} />
            <Route path="*" element={<Navigate to="/not-found" replace />} />
          </Routes>
        </Suspense>
      </AuthProvider>
    </BrowserRouter>
  );
}
