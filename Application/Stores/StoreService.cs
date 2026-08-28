using Microsoft.EntityFrameworkCore;
using SamanMobileInsurance.Application.Abstractions;
using SamanMobileInsurance.Application.Common;
using SamanMobileInsurance.Domain.Entities;
using SamanMobileInsurance.Domain.Enums;

namespace SamanMobileInsurance.Application.Stores;

public class StoreService
{
    private readonly IApplicationDbContext _db;
    private readonly IPasswordHasherService _passwordHasher;
    private readonly IAuditLogger _audit;

    public StoreService(IApplicationDbContext db, IPasswordHasherService passwordHasher, IAuditLogger audit)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _audit = audit;
    }

    public async Task<StoreProfileDto> RegisterAsync(RegisterStoreRequest request, CancellationToken cancellationToken)
    {
        var username = request.Username.Trim();
        if (await _db.Users.AnyAsync(u => u.Username == username && !u.IsDeleted, cancellationToken))
        {
            throw new ConflictException("نام کاربری تکراری است.");
        }

        if (await _db.Stores.AnyAsync(s => s.NationalCode == request.NationalCode && !s.IsDeleted, cancellationToken))
        {
            throw new ConflictException("کد ملی تکراری است.");
        }

        var city = await _db.Cities.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == request.CityId, cancellationToken)
            ?? throw new ValidationAppException("شهر انتخاب‌شده معتبر نیست.");

        if (city.ProvinceId != request.ProvinceId)
        {
            throw new ValidationAppException("شهر با استان انتخاب‌شده مطابقت ندارد.");
        }

        var now = DateTimeOffset.UtcNow;
        var storeId = await _db.ExecuteInTransactionAsync(async ct =>
        {
            var user = new User
            {
                Username = username,
                PasswordHash = _passwordHasher.Hash(request.Password),
                Role = UserRole.Store,
                IsActive = true,
                CreatedAt = now
            };
            _db.Users.Add(user);

            var store = new Store
            {
                StoreName = request.StoreName.Trim(),
                ManagerFirstName = request.ManagerFirstName.Trim(),
                ManagerLastName = request.ManagerLastName.Trim(),
                NationalCode = request.NationalCode,
                BirthDate = request.BirthDate,
                Mobile1 = request.Mobile1,
                Mobile2 = string.IsNullOrWhiteSpace(request.Mobile2) ? null : request.Mobile2,
                ProvinceId = request.ProvinceId,
                CityId = request.CityId,
                Address = request.Address.Trim(),
                PostalCode = request.PostalCode,
                Username = username,
                UserId = user.Id,
                IsActive = true,
                CreatedAt = now
            };
            _db.Stores.Add(store);
            await _db.SaveChangesAsync(ct);
            return store.Id;
        }, cancellationToken);

        await _audit.LogAsync("store-register", nameof(Store), storeId.ToString(), cancellationToken);
        return await GetByIdAsync(storeId, cancellationToken);
    }

    public async Task<StoreProfileDto> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var store = await _db.Stores.AsNoTracking()
            .Include(s => s.Province)
            .Include(s => s.City)
            .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted, cancellationToken)
            ?? throw new NotFoundException("فروشگاه یافت نشد.");

        return Map(store);
    }

    public async Task<StoreProfileDto> GetCurrentAsync(ICurrentUser current, CancellationToken cancellationToken)
    {
        if (current.StoreId is null)
        {
            throw new ForbiddenAppException();
        }

        return await GetByIdAsync(current.StoreId.Value, cancellationToken);
    }

    public static StoreProfileDto Map(Store store) => new(
        store.Id,
        store.StoreName,
        store.ManagerFirstName,
        store.ManagerLastName,
        store.NationalCode,
        store.BirthDate,
        store.Mobile1,
        store.Mobile2,
        store.ProvinceId,
        store.Province.Name,
        store.CityId,
        store.City.Name,
        store.Address,
        store.PostalCode,
        store.Username,
        store.IsActive,
        store.CreatedAt);
}
