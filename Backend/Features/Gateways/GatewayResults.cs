namespace Backend.Features.Gateways;

public enum GatewayOutcome
{
    Ok,
    NotFound,
    Forbidden
}

public sealed record GatewayUpdateResult(GatewayOutcome Outcome, Dtos.GatewayDto? v)
{
    public static GatewayUpdateResult NotFound => new(GatewayOutcome.NotFound, null);
    public static GatewayUpdateResult Forbidden => new(GatewayOutcome.Forbidden, null);
    public static GatewayUpdateResult Ok(Dtos.GatewayDto? v) => new(GatewayOutcome.Ok, v);
}

public sealed record GatewayDeleteResult(GatewayOutcome Outcome)
{
    public static GatewayDeleteResult NotFound => new(GatewayOutcome.NotFound);
    public static GatewayDeleteResult Forbidden => new(GatewayOutcome.Forbidden);
    public static GatewayDeleteResult Ok => new(GatewayOutcome.Ok);
}