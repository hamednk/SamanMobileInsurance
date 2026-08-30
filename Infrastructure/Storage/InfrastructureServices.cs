using Microsoft.Extensions.Logging;
using SamanMobileInsurance.Application.Abstractions;
using SamanMobileInsurance.Application.Payments;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;

namespace SamanMobileInsurance.Infrastructure.Storage;

public class LocalFileStorageService : IFileStorageService
{
    private readonly string _root;

    public LocalFileStorageService(StorageOptions options)
    {
        _root = Path.GetFullPath(options.RootPath);
        Directory.CreateDirectory(_root);
    }

    public async Task<StoredFile> SaveAsync(Stream content, string fileName, string contentType, string folder, CancellationToken cancellationToken = default)
    {
        var safeFolder = folder.Replace("..", string.Empty).Replace('\\', '/');
        var directory = Path.Combine(_root, safeFolder.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(directory);
        var safeName = $"{Guid.NewGuid():N}_{Path.GetFileName(fileName)}";
        var full = Path.Combine(directory, safeName);
        await using var fs = File.Create(full);
        await content.CopyToAsync(fs, cancellationToken);
        var relative = Path.GetRelativePath(_root, full).Replace('\\', '/');
        return new StoredFile(relative, safeName, contentType);
    }

    public Task<Stream> OpenReadAsync(string path, CancellationToken cancellationToken = default)
    {
        var full = Resolve(path);
        Stream stream = File.OpenRead(full);
        return Task.FromResult(stream);
    }

    public Task DeleteAsync(string path, CancellationToken cancellationToken = default)
    {
        var full = Resolve(path);
        if (File.Exists(full))
        {
            File.Delete(full);
        }

        return Task.CompletedTask;
    }

    private string Resolve(string path)
    {
        var full = Path.GetFullPath(Path.Combine(_root, path.Replace('/', Path.DirectorySeparatorChar)));
        if (!full.StartsWith(_root, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Invalid storage path.");
        }

        return full;
    }
}

public class StorageOptions
{
    public string RootPath { get; set; } = Path.Combine(AppContext.BaseDirectory, "data", "uploads");
    public long MaxImageSizeBytes { get; set; } = 5 * 1024 * 1024;
}

public class ImageProcessor : IImageProcessor
{
    public async Task<ProcessedImage> ProcessAsync(Stream input, string contentType, CancellationToken cancellationToken = default)
    {
        using var image = await Image.LoadAsync(input, cancellationToken);
        image.Mutate(x => x.AutoOrient());

        const int max = 1280;
        if (image.Width > max || image.Height > max)
        {
            image.Mutate(x => x.Resize(new ResizeOptions
            {
                Mode = ResizeMode.Max,
                Size = new Size(max, max)
            }));
        }

        var output = new MemoryStream();
        await image.SaveAsJpegAsync(output, new JpegEncoder { Quality = 72 }, cancellationToken);
        output.Position = 0;
        return new ProcessedImage(output, "image/jpeg", ".jpg");
    }
}

public class MockNotificationService : INotificationService
{
    private readonly ILogger _logger;

    public MockNotificationService(ILogger<MockNotificationService> logger) => _logger = logger;

    public Task SendPasswordResetAsync(string destination, string resetToken, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Password reset token generated for {Destination}. Token is not logged.", destination);
        return Task.CompletedTask;
    }
}

public class MockPaymentGateway : IPaymentGateway
{
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, decimal> Authorities = new();

    public MockPaymentGateway(PaymentOptions options) => ArgumentNullException.ThrowIfNull(options);

    public Domain.Enums.PaymentGatewayType GatewayType => Domain.Enums.PaymentGatewayType.Mock;

    public Task<PaymentInitResult> InitiateAsync(Guid paymentId, decimal amountRial, string description, string callbackUrl, CancellationToken cancellationToken = default)
    {
        var authority = Guid.NewGuid().ToString("N");
        Authorities[authority] = amountRial;
        // Relative URL so the store stays on the same origin (not API localhost).
        var redirect = $"/insurance/mock-gateway?authority={authority}";
        return Task.FromResult(new PaymentInitResult(authority, redirect));
    }

    public Task<PaymentVerifyResult> VerifyAsync(string authority, decimal amountRial, CancellationToken cancellationToken = default)
    {
        if (!Authorities.TryGetValue(authority, out var expected) || expected != amountRial)
        {
            return Task.FromResult(new PaymentVerifyResult(false, null, null, "تراکنش در درگاه تأیید نشد."));
        }

        var tracking = $"TRK{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";
        var tx = $"TXN{authority[..8].ToUpperInvariant()}";
        return Task.FromResult(new PaymentVerifyResult(true, tracking, tx, null));
    }
}
