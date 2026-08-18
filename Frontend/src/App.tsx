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
import { DeviceProfilesListPage } from "@/features/deviceProfiles/pages/DeviceProfilesListPage";
import { CreateDeviceProfilePage } from "@/features/deviceProfiles/pages/CreateDeviceProfilePage";
import { EditDeviceProfilePage } from "@/features/deviceProfiles/pages/EditDeviceProfilePage";
import { GatewayListPage } from "./features/gateways/pages/GatewayListPage";
import { CreateGatewayPage } from "./features/gateways/pages/CreateGatewayPage";
import { EditGatewayPage } from "./features/gateways/pages/EditGatewayPage";
import { NodesListPage } from "./features/nodes/pages/NodesListPage";
import { ActivateNodePage } from "./features/nodes/pages/ActivateNodePage";
import { NodeDetailPage } from "./features/nodes/pages/NodeDetailPage";
import { ProvisioningListPage } from "./features/nodes/pages/ProvisioningListPage";
import { EditProvisioningPage } from "./features/nodes/pages/EditProvisioningPage";
import { ProvisionNodePage } from "./features/nodes/pages/ProvisionNodePage";

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
          {/* User paths */}
          <Route path="/users" element={<UsersListPage />} />
          <Route path="/users/new" element={<CreateUserPage />} />
          {/* Organisation paths */}
          <Route
            path="/organisations/new"
            element={<CreateOrganisationPage />}
          />
          <Route path="/organisations" element={<div>Organisations</div>} />

          <Route path="/alarms" element={<div>Alarms</div>} />
          {/* Device profile paths */}
          <Route path="/device-profiles" element={<DeviceProfilesListPage />} />
          <Route
            path="/device-profiles/new"
            element={<CreateDeviceProfilePage />}
          />
          <Route
            path="/device-profiles/:id/edit"
            element={<EditDeviceProfilePage />}
          />
          <Route path="/users/:id/edit" element={<EditUserPage />} />
          {/* Gateway paths */}
          <Route path="/gateways" element={<GatewayListPage />} />
          <Route path="/gateways/new" element={<CreateGatewayPage />} />
          <Route path="/gateways/:eui/edit" element={<EditGatewayPage />} />
          {/* Node paths */}
          <Route path="/nodes" element={<NodesListPage />} />
          <Route path="/nodes/activate" element={<ActivateNodePage />} />
          <Route path="/nodes/:devEui" element={<NodeDetailPage />} />
          <Route
            path="/provisioning/nodes/:id/edit"
            element={<EditProvisioningPage />}
          />
          <Route
            path="/provisioning/nodes/new"
            element={<ProvisionNodePage />}
          />
          <Route
            path="/provisioning/nodes"
            element={<ProvisioningListPage />}
          />
        </Route>
      </Route>
    </Routes>
  );
}
