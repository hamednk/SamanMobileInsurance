using Microsoft.EntityFrameworkCore;
using SamanMobileInsurance.Application.Abstractions;
using SamanMobileInsurance.Application.Common;
using SamanMobileInsurance.Domain.Entities;
using SamanMobileInsurance.Domain.Enums;

namespace SamanMobileInsurance.Application.Insurance;

public class InsuranceImageService
{
    private static readonly HashSet<string> AllowedTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/jpg", "image/png", "image/webp"
    };

    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _current;
    private readonly IFileStorageService _storage;
    private readonly IImageProcessor _images;
    private readonly IAuditLogger _audit;

    public InsuranceImageService(
        IApplicationDbContext db,
        ICurrentUser current,
        IFileStorageService storage,
        IImageProcessor images,
        IAuditLogger audit)
    {
        _db = db;
        _current = current;
        _storage = storage;
        _images = images;
        _audit = audit;
    }

    public async Task<PolicyDto> UploadAsync(
        Guid policyId,
        ImageType imageType,
        Stream content,
        string fileName,
        string contentType,
        long length,
        CancellationToken cancellationToken)
    {
        if (!AllowedTypes.Contains(contentType))
        {
            throw new ValidationAppException("فقط فایل‌های JPG، PNG و WEBP مجاز هستند.");
        }

        var maxBytes = 5 * 1024 * 1024;
        if (length <= 0 || length > maxBytes)
        {
            throw new ValidationAppException("حجم تصویر بیش از حد مجاز است.");
        }

        var policy = await _db.InsurancePolicies
            .Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.Id == policyId, cancellationToken)
            ?? throw new NotFoundException("بیمه‌نامه یافت نشد.");

        EnsureAccess(policy);

        if (policy.Status is not PolicyStatus.AwaitingImages and not PolicyStatus.Draft)
        {
            throw new BusinessRuleException("در این وضعیت امکان بارگذاری تصویر وجود ندارد.");
        }

        var previousStatus = policy.Status;

        await using var processed = (await _images.ProcessAsync(content, contentType, cancellationToken)).Content;
        processed.Position = 0;
        var stored = await _storage.SaveAsync(
            processed,
            $"{imageType.ToString().ToLowerInvariant()}.jpg",
            "image/jpeg",
            $"policies/{policy.Id}",
            cancellationToken);

        var existing = policy.Images.FirstOrDefault(i => i.ImageType == imageType);
        if (existing is not null)
        {
            await _storage.DeleteAsync(existing.FilePath, cancellationToken);
            existing.FilePath = stored.Path;
            existing.FileName = stored.FileName;
            existing.ContentType = stored.ContentType;
            existing.UploadedAt = DateTimeOffset.UtcNow;
            existing.UpdatedAt = DateTimeOffset.UtcNow;
        }
        else
        {
            _db.InsuranceImages.Add(new InsuranceImage
            {
                PolicyId = policy.Id,
                ImageType = imageType,
                FilePath = stored.Path,
                FileName = stored.FileName,
                ContentType = stored.ContentType,
                UploadedAt = DateTimeOffset.UtcNow,
                CreatedAt = DateTimeOffset.UtcNow
            });
        }

        // Avoid marking InsurancePolicy Modified (RowVersion concurrency). Persist images only.
        await _db.SaveChangesAsync(cancellationToken);

        var hasFront = await _db.InsuranceImages.AnyAsync(
            i => i.PolicyId == policyId && i.ImageType == ImageType.Front, cancellationToken);
        var hasBack = await _db.InsuranceImages.AnyAsync(
            i => i.PolicyId == policyId && i.ImageType == ImageType.Back, cancellationToken);

        if (hasFront && hasBack)
        {
            await _db.InsurancePolicies
                .Where(p => p.Id == policyId &&
                            (p.Status == PolicyStatus.AwaitingImages || p.Status == PolicyStatus.Draft))
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(p => p.Status, PolicyStatus.AwaitingPayment)
                        .SetProperty(p => p.UpdatedAt, DateTimeOffset.UtcNow),
                    cancellationToken);
        }
        else if (previousStatus == PolicyStatus.Draft)
        {
            await _db.InsurancePolicies
                .Where(p => p.Id == policyId && p.Status == PolicyStatus.Draft)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(p => p.Status, PolicyStatus.AwaitingImages)
                        .SetProperty(p => p.UpdatedAt, DateTimeOffset.UtcNow),
                    cancellationToken);
        }

        await _audit.LogAsync("policy-image-upload", nameof(InsurancePolicy), policy.Id.ToString(), cancellationToken);
        return await ReloadAsync(policy.Id, cancellationToken);
    }

    public async Task<(Stream Stream, string ContentType, string FileName)> OpenAsync(
        Guid policyId,
        Guid imageId,
        CancellationToken cancellationToken)
    {
        var image = await _db.InsuranceImages.Include(i => i.Policy)
            .FirstOrDefaultAsync(i => i.Id == imageId && i.PolicyId == policyId, cancellationToken)
            ?? throw new NotFoundException("تصویر یافت نشد.");

        EnsureAccess(image.Policy);
        var stream = await _storage.OpenReadAsync(image.FilePath, cancellationToken);
        return (stream, image.ContentType, image.FileName);
    }

    private void EnsureAccess(InsurancePolicy policy)
    {
        if (_current.IsStore && policy.StoreId != _current.StoreId)
        {
            throw new ForbiddenAppException();
        }
    }

    private async Task<PolicyDto> ReloadAsync(Guid id, CancellationToken cancellationToken)
    {
        var policy = await _db.InsurancePolicies.AsNoTracking()
            .Include(p => p.Store)
            .Include(p => p.Customer)
            .Include(p => p.Brand)
            .Include(p => p.Model)
            .Include(p => p.Images)
            .FirstAsync(p => p.Id == id, cancellationToken);
        return InsuranceService.Map(policy);
    }
}
