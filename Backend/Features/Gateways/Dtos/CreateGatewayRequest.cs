using System.ComponentModel.DataAnnotations;

namespace Backend.Features.Gateways.Dtos;

public record CreateGatewayRequest(
    [property: Required, RegularExpression("^[0-9a-fA-F]{16}$",
    ErrorMessage = "Gateway EUI must be a 16-character hexadecimal string.")]
    string GatewayEui,
    [property: Required, MaxLength(100)] string Alias,
    [property: Required, MaxLength(100)] string Model,
    double? Latitude,
    double? Longitude);