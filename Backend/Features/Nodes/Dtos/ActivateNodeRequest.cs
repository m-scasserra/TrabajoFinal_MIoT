using System.ComponentModel.DataAnnotations;

namespace Backend.Features.Nodes.Dtos;

public record ActivateNodeRequest(
    [property: Required, RegularExpression(@"^[A-Fa-f0-9]{12}$",
        ErrorMessage = "Invalid MAC address format")]
    string MacAddress,
    [property: Required] string DeviceProfileId,
    [property: Required, MaxLength(100)] string Alias,
    [property: Required] string MeterType,
    int? FreqMinutes,
    double? Latitude,
    double? Longitude);