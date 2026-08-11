using System.ComponentModel.DataAnnotations;

namespace Backend.Features.Nodes.Dtos;

public record ProvisionNodeResponse(
    Guid Id,
    string DevEui,
    string MacAddress,
    int HwRevision,
    int FwRevision,
    string AppKey);