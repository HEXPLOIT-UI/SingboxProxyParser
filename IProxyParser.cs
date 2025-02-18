namespace SingboxProxyParser;

public interface IProxyParser
{
    Task<List<string>> ParseProxiesAsync(string url, CancellationToken ct);
}