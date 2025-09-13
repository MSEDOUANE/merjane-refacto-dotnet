using Microsoft.EntityFrameworkCore;
using Refacto.DotNet.Controllers.Database.Context;
using Refacto.DotNet.Controllers.Dtos.Product;
using Refacto.DotNet.Controllers.Entities;
using Refacto.DotNet.Controllers.Services;

namespace Refacto.DotNet.Controllers.Services.Impl
{
    public class OrderService : IOrderService
    {
        private readonly AppDbContext _ctx;
        private readonly IEnumerable<IProductTypeHandler> _handlers;

        public OrderService(AppDbContext ctx, IEnumerable<IProductTypeHandler> handlers)
        {
            _ctx = ctx;
            _handlers = handlers;
        }
        /// <summary>
        /// Processes the specified order by handling its associated items using the appropriate product type handlers.
        /// </summary>
        /// <param name="orderId">The unique identifier of the order to process.</param>
        /// <param name="ct">A <see cref="CancellationToken"/> that can be used to cancel the operation.</param>
        /// <returns>A <see cref="ProcessOrderResponse"/> containing the processed order's details, or <see langword="null"/> if
        /// the order does not exist.</returns>
        public async Task<ProcessOrderResponse?> ProcessOrderAsync(long orderId, CancellationToken ct = default)
        {
            // cant apply AsNoTracking because we need to update the products
            Order? order = await _ctx.Orders
                .Include(o => o.Items)
                .SingleOrDefaultAsync(o => o.Id == orderId, ct);

            if (order is null)
            {
                return null;
            }
            // Process each item in the order using the appropriate handler
            if (order.Items is not null && order.Items.Count > 0)
            {
                List<Task> tasks = new(order.Items.Count);

                foreach (Product p in order.Items)
                {
                    IProductTypeHandler? handler = _handlers.FirstOrDefault(h => h.CanHandle(p));
                    if (handler is not null)
                    {
                        tasks.Add(handler.HandleAsync(p, ct));
                    }
                }
                await Task.WhenAll(tasks);
            }

            return new ProcessOrderResponse(order.Id);
        }
    }
}