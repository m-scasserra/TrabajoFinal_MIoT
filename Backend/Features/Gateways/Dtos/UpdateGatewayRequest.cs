using System.ComponentModel.DataAnnotations;

namespace Backend.Features.Gateways.Dtos;

public record UpdateGatewayRequest(
    [property: Required, MaxLength(100)] string Alias,
    [property: Required, MaxLength(100)] string Model,
    [property: Required] string OperativeState,
    double? Latitude,
    double? Longitude
);