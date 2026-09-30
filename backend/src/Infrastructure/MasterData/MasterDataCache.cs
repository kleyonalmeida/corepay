using Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Infrastructure.MasterData;

public sealed class MasterDataCache(AppDbContext dbContext, IMemoryCache memoryCache)
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(10);

    public async Task<IReadOnlyList<Department>> GetActiveDepartmentsAsync(CancellationToken cancellationToken = default) =>
        await memoryCache.GetOrCreateAsync(
            "masterdata:departments:active",
            async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = CacheDuration;
                return (IReadOnlyList<Department>)await dbContext.Departments
                    .AsNoTracking()
                    .Where(department => department.IsActive)
                    .OrderBy(department => department.Name)
                    .ToListAsync(cancellationToken);
            }) ?? [];

    public async Task<IReadOnlyList<Project>> GetActiveProjectsAsync(CancellationToken cancellationToken = default) =>
        await memoryCache.GetOrCreateAsync(
            "masterdata:projects:active",
            async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = CacheDuration;
                return (IReadOnlyList<Project>)await dbContext.Projects
                    .AsNoTracking()
                    .Where(project => project.IsActive)
                    .OrderBy(project => project.Name)
                    .ToListAsync(cancellationToken);
            }) ?? [];

    public async Task<IReadOnlyList<CareerLevel>> GetActiveCareerLevelsAsync(CancellationToken cancellationToken = default) =>
        await memoryCache.GetOrCreateAsync(
            "masterdata:careerlevels:active",
            async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = CacheDuration;
                return (IReadOnlyList<CareerLevel>)await dbContext.CareerLevels
                    .AsNoTracking()
                    .Where(level => level.IsActive)
                    .OrderBy(level => level.Name)
                    .ToListAsync(cancellationToken);
            }) ?? [];

    public async Task<IReadOnlyList<PaymentMethod>> GetActivePaymentMethodsAsync(CancellationToken cancellationToken = default) =>
        await memoryCache.GetOrCreateAsync(
            "masterdata:paymentmethods:active",
            async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = CacheDuration;
                return (IReadOnlyList<PaymentMethod>)await dbContext.PaymentMethods
                    .AsNoTracking()
                    .Where(method => method.IsActive)
                    .OrderBy(method => method.Name)
                    .ToListAsync(cancellationToken);
            }) ?? [];

    public void InvalidateAll()
    {
        memoryCache.Remove("masterdata:departments:active");
        memoryCache.Remove("masterdata:projects:active");
        memoryCache.Remove("masterdata:careerlevels:active");
        memoryCache.Remove("masterdata:paymentmethods:active");
    }
}
