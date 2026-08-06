import { Routes, Route } from "react-router";
import { ProtectedRoute } from "./routes/ProtectedRoute";
import { AppShell } from "@/components/layout/AppShell";
import { TopBarProvider } from "@/components/layout/TopBarContext";
import { LoginPage } from "./features/auth/pages/LoginPage";
import { ConfirmAccountPage } from "./features/auth/pages/ConfirmAccountPage";
import { UsersListPage } from "./features/users/pages/UsersListPage";
import { CreateUserPage } from "./features/users/pages/CreateUserPage";
import { CreateOrganisationPage } from "./features/organisations/pages/CreateOrganisationPage";
import { EditUserPage } from "./features/users/pages/EditUserPage";
//import { Layout } from "./components/Layout";

export default function App() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route path="/confirm-account" element={<ConfirmAccountPage />} />

      <Route element={<ProtectedRoute />}>
        <Route
          element={
            <TopBarProvider>
              <AppShell />
            </TopBarProvider>
          }
        >
          <Route path="/" element={<div>Home</div>} />
          <Route path="/users" element={<UsersListPage />} />
          <Route path="/users/new" element={<CreateUserPage />} />
          <Route
            path="/organisations/new"
            element={<CreateOrganisationPage />}
          />
          <Route path="/organisations" element={<div>Organisations</div>} />
          <Route path="/devices" element={<div>Devices</div>} />
          <Route path="/alarms" element={<div>Alarms</div>} />
          <Route path="/users/:id/edit" element={<EditUserPage />} />
        </Route>
      </Route>
    </Routes>
  );
}
