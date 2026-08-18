import { useParams, useNavigate } from "react-router";
import { useAuth } from "@/features/auth/AuthContext";
import { Roles } from "@/features/users/roles";
import { useDeviceProfile } from "../hooks/useDeviceProfile";
import { useSaveDeviceProfile } from "../hooks/useSaveDeviceProfile";
import { useSetTopBar } from "@/components/layout/TopBarContext";
import { AsyncBoundary } from "@/components/AsyncBoundary";
import { DeviceProfileForm } from "../components/DeviceProfileForm";
import { DeleteDeviceProfileButton } from "../components/DeleteDeviceProfileButton";
import type { DeviceProfilePayload } from "@/api/endpoints/deviceProfiles";

export function EditDeviceProfilePage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { user } = useAuth();
  const { profile, loading, error: loadError } = useDeviceProfile(id);
  const { save, saving, error: saveError } = useSaveDeviceProfile();

  useSetTopBar("Edit Device Profile");

  if (user?.role !== Roles.SuperAdmin) {
    return (
      <p className="text-sm text-destructive bg-destructive/10 border border-destructive/20 rounded-md px-4 py-3">
        You do not have permission to edit a device profile.
      </p>
    );
  }

  const handleSubmit = async (payload: DeviceProfilePayload) => {
    if (!id) return;
    const result = await save(payload, id);
    if (result) navigate("/device-profiles");
  };

  return (
    <div className="max-w-2xl">
      <AsyncBoundary loading={loading} error={loadError}>
        {profile && (
          <div className="space-y-4">
            <DeviceProfileForm
              initial={profile}
              saving={saving}
              error={saveError}
              submitLabel="Save changes"
              onSubmit={handleSubmit}
              onCancel={() => navigate("/device-profiles")}
            />

            <div className="border border-destructive/30 rounded-lg p-4">
              <h3 className="text-sm font-medium text-foreground mb-1">
                Delete Device Profile
              </h3>
              <p className="text-xs text-muted-foreground mb-3">
                You can't Delete a device profile if there are devices
                associated with it. Please make sure to reassign or delete any
                devices using this profile before proceeding.
              </p>
              <DeleteDeviceProfileButton
                id={profile.id}
                name={profile.name}
                onDeleted={() => navigate("/device-profiles")}
              />
            </div>
          </div>
        )}
      </AsyncBoundary>
    </div>
  );
}
