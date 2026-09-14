namespace Ingest.Core.Decoding;

public enum DecodeError
{
    Empty,
    Malformed,
    UnsupportedMessageType,
    MissingConfig,
}

public sealed class DecodeResult
{
    public bool IsSuccess { get; }
    public DecodedMessage? Message { get; }
    public DecodeError? Error { get; }
    public string? ErrorDetail { get; }

    private DecodeResult(bool ok, DecodedMessage? message, DecodeError? error, string? errorDetail)
    {
        IsSuccess = ok;
        Message = message;
        Error = error;
        ErrorDetail = errorDetail;
    }

    public static DecodeResult Success(DecodedMessage message) =>
        new DecodeResult(true, message, null, null);

    public static DecodeResult Failure(DecodeError error, string detail) =>
    new DecodeResult(false, null, error, detail);
}