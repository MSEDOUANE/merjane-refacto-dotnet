using Refacto.DotNet.Controllers.Dtos.Product;
using System.Threading;

namespace Refacto.DotNet.Controllers.Services
{
    public interface IOrderService
    {
        Task<ProcessOrderResponse?> ProcessOrderAsync(long orderId, CancellationToken ct = default);
    }
}