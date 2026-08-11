using System.ComponentModel.DataAnnotations;

namespace Backend.Features.Nodes.Dtos;

public record ProvisionNodeRequest(
    [property: Required, RegularExpression(@"^[A-Fa-f0-9]{12}$",
        ErrorMessage = "Invalid MAC address format.")]
    string MacAddress,
    [property: Required, Range(0, 255)] int HwRevision,
    [property: Required, Range(0, 255)] int FwRevision
);