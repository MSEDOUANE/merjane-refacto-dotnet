using Refacto.DotNet.Controllers.Entities;
using System.Threading;

namespace Refacto.DotNet.Controllers.Services
{
    public interface IProductTypeHandler
    {
        /// <summary>
        /// Determines whether the specified product can be handled by the current handler.
        /// </summary>
        /// <param name="p">The product to evaluate. Cannot be <see langword="null"/>.</param>
        /// <returns><see langword="true"/> if the product can be handled; otherwise, <see langword="false"/>.</returns>
        bool CanHandle(Product p);
        /// <summary>
        /// Handles the specified product asynchronously.
        /// </summary>
        /// <param name="p">The product to be processed. Cannot be <see langword="null"/>.</param>
        /// <param name="ct">A <see cref="CancellationToken"/> that can be used to cancel the operation. Defaults to <see
        /// cref="CancellationToken.None"/>.</param>
        /// <returns>A <see cref="Task"/> that represents the asynchronous operation.</returns>
        Task HandleAsync(Product p, CancellationToken ct = default);
    }
}