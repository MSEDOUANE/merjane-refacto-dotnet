using Refacto.DotNet.Controllers.Database.Context;
using Refacto.DotNet.Controllers.Entities;
using Refacto.DotNet.Controllers.Services;

namespace Refacto.DotNet.Controllers.Services.Impl
{
    public class ProductService : IProductService
    {
        private readonly INotificationService _ns;
        private readonly AppDbContext _ctx;

        public ProductService(INotificationService ns, AppDbContext ctx)
        {
            _ns = ns;
            _ctx = ctx;
        }

        public async Task NotifyDelayAsync(int leadTime, Product p, CancellationToken ct = default)
        {
            p.LeadTime = leadTime;
            _ = await _ctx.SaveChangesAsync(ct);
            _ns.SendDelayNotification(leadTime, p.Name);
        }

        /// <summary>
        /// Handles the processing of a normal product by decrementing its availability or notifying of a delay.
        /// </summary>
        /// <param name="p">The product to be processed. Must not be <see langword="null"/>.</param>
        /// <param name="ct">An optional <see cref="CancellationToken"/> to observe while performing the operation.</param>
        /// <returns></returns>
        public async Task HandleNormalProductAsync(Product p, CancellationToken ct = default)
        {
            if (p.Available > 0)
            {
                p.Available -= 1;
                _ = await _ctx.SaveChangesAsync(ct);
            }
            else
            {
                if (p.LeadTime > 0)
                {
                    await NotifyDelayAsync(p.LeadTime, p, ct);
                }
            }
        }

        /// <summary>
        /// Handles the availability and notifications for a seasonal product based on its current state and season
        /// dates.
        /// </summary>
        /// <param name="p">The product to be processed. The product must have valid season start and end dates, as well as a lead time.</param>
        /// <param name="ct">An optional <see cref="CancellationToken"/> to observe while waiting for the operation to complete.</param>
        /// <returns></returns>
        public async Task HandleSeasonalProductAsync(Product p, CancellationToken ct = default)
        {
            if (p.Available > 0 && DateTime.Now > p.SeasonStartDate && DateTime.Now < p.SeasonEndDate)
            {
                p.Available -= 1;
                _ = await _ctx.SaveChangesAsync(ct);
                return;
            }

            if (DateTime.Now.AddDays(p.LeadTime) > p.SeasonEndDate)
            {
                _ns.SendOutOfStockNotification(p.Name);
                p.Available = 0;
                _ = await _ctx.SaveChangesAsync(ct);
            }
            else if (p.SeasonStartDate > DateTime.Now)
            {
                _ns.SendOutOfStockNotification(p.Name);
                _ = await _ctx.SaveChangesAsync(ct);
            }
            else
            {
                await NotifyDelayAsync(p.LeadTime, p, ct);
            }
        }


        /// <summary>
        /// Handles the expiration of a product by updating its availability and sending a notification if necessary.
        /// </summary>
        /// <param name="p">The product to be processed. The product's availability and expiration date are used to determine the
        /// action.</param>
        /// <param name="ct">An optional <see cref="CancellationToken"/> to observe while performing the operation.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task HandleExpiredProductAsync(Product p, CancellationToken ct = default)
        {
            if (p.Available > 0 && p.ExpiryDate > DateTime.Now)
            {
                p.Available -= 1;
                _ = await _ctx.SaveChangesAsync(ct);
            }
            else
            {
                _ns.SendExpirationNotification(p.Name, (DateTime)p.ExpiryDate);
                p.Available = 0;
                _ = await _ctx.SaveChangesAsync(ct);
            }
        }
    }
}
