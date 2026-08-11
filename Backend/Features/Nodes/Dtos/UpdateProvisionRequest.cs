using System.ComponentModel.DataAnnotations;

namespace Backend.Features.Nodes.Dtos;

public record UpdateProvisionRequest(
    [property: Required, Range(0, 255)] int HwRevision,
    [property: Required, Range(0, 255)] int FwRevision);