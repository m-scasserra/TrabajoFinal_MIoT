using Backend.Features.DeviceProfiles.Dtos;

namespace Backend.Features.DeviceProfiles;

public interface IDeviceProfileService
{
    Task<IEnumerable<DeviceProfileDto>> ListAsync();
    Task<DeviceProfileDto?> GetAsync(Guid id);
    Task<DeviceProfileDto> CreateAsync(CreateDeviceProfileRequest req, CancellationToken ct = default);
    Task<DeviceProfileDto?> UpdateAsync(Guid id, CreateDeviceProfileRequest req, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
    Task<int> ReconcilePendingAsync(CancellationToken ct = default);
}