using Refacto.DotNet.Controllers.Entities;
using System.Threading;

namespace Refacto.DotNet.Controllers.Services
{
    public interface IProductService
    {
        Task NotifyDelayAsync(int leadTime, Product p, CancellationToken ct = default);
        Task HandleNormalProductAsync(Product p, CancellationToken ct = default);
        Task HandleSeasonalProductAsync(Product p, CancellationToken ct = default);
        Task HandleExpiredProductAsync(Product p, CancellationToken ct = default);
    }
}