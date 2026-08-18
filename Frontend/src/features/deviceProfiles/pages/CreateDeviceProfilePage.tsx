import { useNavigate } from "react-router";
import { useAuth } from "@/features/auth/AuthContext";
import { Roles } from "@/features/users/roles";
import { useSaveDeviceProfile } from "../hooks/useSaveDeviceProfile";
import { useSetTopBar } from "@/components/layout/TopBarContext";
import type { DeviceProfilePayload } from "@/api/endpoints/deviceProfiles";
import { DeviceProfileForm } from "../components/DeviceProfileForm";

export function CreateDeviceProfilePage() {
  const { user } = useAuth();
  const navigate = useNavigate();
  const { save, saving, error } = useSaveDeviceProfile();

  useSetTopBar("Create Device Profile");

  if (user?.role !== Roles.SuperAdmin) {
    return (
      <p className="text-sm text-destructive bg-destructive/10 border border-destructive/20 rounded-md px-4 py-3">
        You do not have permission to create a device profile.
      </p>
    );
  }

  const handleSubmit = async (payload: DeviceProfilePayload) => {
    const result = await save(payload);
    if (result) navigate("/device-profiles");
  };

  return (
    <div className="max-w-2xl">
      <DeviceProfileForm
        saving={saving}
        error={error}
        submitLabel="Create device profile"
        onSubmit={handleSubmit}
        onCancel={() => navigate("/device-profiles")}
      />
    </div>
  );
}
