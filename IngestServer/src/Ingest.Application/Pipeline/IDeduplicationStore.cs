namespace Ingest.Application.Pipeline;

public interface IDeduplicationStore
{
    bool TryRegister(string devEui, uint frameCounter);
}