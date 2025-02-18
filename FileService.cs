using Microsoft.Extensions.Logging;
using System.Buffers;
using System.Security.Cryptography;
using System.Text;

namespace SingboxProxyParser;

public interface IFileService
{
    Task RemoveDuplicatedStringsAsync(string fileName);
}

public class FileService : IFileService
{
    private readonly ILogger<FileService> _logger;

    public FileService(ILogger<FileService> logger)
    {
        _logger = logger;
    }

    public async Task RemoveDuplicatedStringsAsync(string fileName)
    {
        _logger.LogInformation("Removing duplicates...");
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
                    int count = 0;
                    // Чтение блока строк
                    for (; count < buffer.Length && !reader.EndOfStream; count++)
                    {
                        buffer[count] = await reader.ReadLineAsync() ?? string.Empty;
                    }

                    // Обработка блока
                    foreach (var line in buffer.Take(count))
                    {
                        if (string.IsNullOrEmpty(line)) continue;

                        // Хеширование с минимальными аллокациями
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
    }
    private static string GetLineHash(string line)
    {
        using var hasher = MD5.Create();
        var hashBytes = hasher.ComputeHash(Encoding.UTF8.GetBytes(line));
        return Convert.ToBase64String(hashBytes);
    }
}
