using System.Text;
using System.Text.RegularExpressions;

namespace SingboxProxyParser;

public class ProxyParser(IHttpClientFactory httpClientFactory) : IProxyParser
{
    private static readonly Regex _proxyRegex = new(
        @"\b(vmess|vless|ss|trojan|socks4|socks5|tuic|hysteria|hysteria2|naive|)://[^\s]+",
        RegexOptions.Compiled);
    //https://github.com/SagerNet/sing-box

    public async Task<List<string>> ParseProxiesAsync(string url, CancellationToken ct)
    {
        using var client = httpClientFactory.CreateClient();
        using var response = await client.GetAsync(url, ct);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Request failed with status code: {response.StatusCode}");
        }

        var content = await response.Content.ReadAsStringAsync(ct);
        return ExtractProxyLinks(content);
    }

    private List<string> ExtractProxyLinks(string content)
    {
        var links = new HashSet<string>();

        if (IsBase64Encoded(content))
        {
            try
            {
                var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(content));
                ExtractLinks(decoded, links);
            }
            catch (FormatException) { /* Ignore invalid Base64 */ }
        }
        else
        {
            ExtractLinks(content, links);
        }

        return links.ToList();
    }

    private static void ExtractLinks(string text, HashSet<string> links)
    {
        foreach (Match match in _proxyRegex.Matches(text))
        {
            links.Add(match.Value);
        }
    }

    private static bool IsBase64Encoded(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return false;
        input = input.Trim();

        return input.Length % 4 == 0 &&
               Regex.IsMatch(input, @"^[A-Za-z0-9+/]*={0,2}$");
    }
}