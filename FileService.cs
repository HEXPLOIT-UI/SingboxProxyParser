using Microsoft.Extensions.Logging;
using System.Buffers;
using System.Security.Cryptography;
using System.Text;

namespace SingboxProxyParser;

public interface IFileService
{
    Task RemoveDuplicatedStringsAsync(string fileName);
}

public class FileService(ILogger<FileService> logger) : IFileService
{
    public async Task RemoveDuplicatedStringsAsync(string fileName)
    {
        logger.LogInformation("Removing duplicates...");
        var tempFile = Path.GetTempFileName();
        var buffer = ArrayPool<string>.Shared.Rent(10000);
        var hashes = new HashSet<string>(capacity: 1_000_000);

        try
        {
            using (var reader = new StreamReader(fileName))
            await using (var writer = new StreamWriter(tempFile))
            {
                while (!reader.EndOfStream)
                {
                    var count = 0;
                    // Reading block of strings
                    for (; count < buffer.Length && !reader.EndOfStream; count++)
                    {
                        buffer[count] = await reader.ReadLineAsync() ?? string.Empty;
                    }

                    // Block processing
                    foreach (var line in buffer.Take(count))
                    {
                        if (string.IsNullOrEmpty(line)) continue;

                        // Hashing 
                        var hash = GetLineHash(line);
                        if (hashes.Add(hash))
                        {
                            await writer.WriteLineAsync(line);
                        }
                    }
                }
            }

            File.Delete(fileName);
            File.Move(tempFile, fileName);
        }
        finally
        {
            ArrayPool<string>.Shared.Return(buffer);
        }
        // Validate proxy link
        string[] validPrefixes =
        [
            "vmess", "vless", "ss", "trojan",
            "socks4", "socks5", "tuic",
            "hysteria", "hysteria2", "naive"
        ];
        var tempFilePath = Path.GetTempFileName();
        using (var reader = new StreamReader(fileName))
        await using (var writer = new StreamWriter(tempFilePath))
        {
            while (await reader.ReadLineAsync() is { } line)
            {
                var isValid = validPrefixes.Any(prefix => line.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
                if (isValid)
                {
                    await writer.WriteLineAsync(line);
                }
            }
        }

        File.Delete(fileName);
        File.Move(tempFilePath, fileName);
    }
    private static string GetLineHash(string line)
    {
        using var hasher = MD5.Create();
        var hashBytes = hasher.ComputeHash(Encoding.UTF8.GetBytes(line));
        return Convert.ToBase64String(hashBytes);
    }
}
