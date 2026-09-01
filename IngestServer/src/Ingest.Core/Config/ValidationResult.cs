namespace Ingest.Core.Config;

public sealed class ValidationResult
{
    private readonly List<string> _errors = [];
    public IReadOnlyList<string> Errors => _errors;
    public bool IsValid => _errors.Count == 0;
    public void Add(string error) => _errors.Add(error);

    public override string ToString() =>
        IsValid ? "Valid config" : string.Join(Environment.NewLine, _errors);
}