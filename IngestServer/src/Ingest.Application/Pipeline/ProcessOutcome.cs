namespace Ingest.Application.Pipeline;

public enum ProcessOutcome
{
    Processed,
    Duplicate,
    UnknownDevice,
    Desynchronized,
    Malformed,
    Unsupported
}