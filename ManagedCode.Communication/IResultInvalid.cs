namespace ManagedCode.Communication;

public interface IResultInvalid
{
    bool IsInvalid { get; }
    void AddInvalidMessage(string message);
    void AddInvalidMessage(string key, string value);
}