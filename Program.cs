using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace SingboxProxyParser;

internal class Program
{
    private static readonly SemaphoreSlim _fileSemaphore = new(1, 1);
    private static readonly string _outputFile = "files/proxies.txt";
    private static readonly string _inputFile = "files/links.txt";
    private static readonly ServiceProvider _serviceProvider;
    private static readonly IProxyParser _proxyParser;
    private static readonly IFileService _fileService;
    private static readonly ILogger<Program> _logger;

    private static volatile int _urlsProcessed;
    private static volatile int _proxiesParsed;

    static Program()
    {
        var services = new ServiceCollection();
        ConfigureServices(services);
        _serviceProvider = services.BuildServiceProvider();
        _proxyParser = _serviceProvider.GetRequiredService<IProxyParser>();
        _fileService = _serviceProvider.GetRequiredService<IFileService>();
        _logger = _serviceProvider.GetRequiredService<ILogger<Program>>();
    }

    static async Task Main()
    {
        Console.OutputEncoding = Encoding.UTF8;
        _logger.LogInformation("Starting proxy parsing...");
        Directory.CreateDirectory("files");
        File.Delete(_outputFile);
        if (!File.Exists(_inputFile))
        {
            await File.WriteAllTextAsync(_inputFile, "https://raw.githubusercontent.com/mahsanet/MahsaFreeConfig/refs/heads/main/mci/sub_4.txt");
        }
        var urls = await File.ReadAllLinesAsync(_inputFile);

        var options = new ParallelOptions { MaxDegreeOfParallelism = 5 };
        await Parallel.ForEachAsync(urls, options, async (url, ct) =>
        {
            try
            {
                await ProcessUrlAsync(url, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
        });

        await _fileService.RemoveDuplicatedStringsAsync(_outputFile);

        _logger.LogInformation($"\nParsing completed. Processed {_urlsProcessed} URLs | Found {_proxiesParsed} proxies");
        _logger.LogInformation("Press any key to continue...");
        Console.ReadKey();
        await _serviceProvider.DisposeAsync();
    }
    

    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddHttpClient<ProxyParser>()
            .SetHandlerLifetime(TimeSpan.FromMinutes(5))
            .ConfigureHttpClient(client =>
            {
                client.Timeout = TimeSpan.FromSeconds(5);
            });
        services.AddLogging(loggingBuilder =>
        {
            loggingBuilder.ClearProviders();
            loggingBuilder.AddConsole()      
                .SetMinimumLevel(LogLevel.Information); 
        });
        services.AddSingleton<IProxyParser, ProxyParser>();
        services.AddSingleton<IFileService, FileService>();
    }

    private static async Task ProcessUrlAsync(string url, CancellationToken ct)
    {
        _logger.LogInformation($"Processing URL: {url}");

        var proxies = await _proxyParser.ParseProxiesAsync(url, ct);
        if (proxies.Count == 0) return;

        await WriteProxiesAsync(proxies);

        Interlocked.Add(ref _proxiesParsed, proxies.Count);
        Interlocked.Increment(ref _urlsProcessed);
    }

    private static async Task WriteProxiesAsync(IReadOnlyCollection<string> proxies)
    {
        await _fileSemaphore.WaitAsync();
        try
        {
            await File.AppendAllLinesAsync(_outputFile, proxies);
        }
        finally
        {
            _fileSemaphore.Release();
        }
    }
}