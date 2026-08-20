namespace Ingest.Core.Protocol;

public sealed class PayloadFormatException : Exception
{
    public PayloadFormatException(string message) : base(message) { }
}