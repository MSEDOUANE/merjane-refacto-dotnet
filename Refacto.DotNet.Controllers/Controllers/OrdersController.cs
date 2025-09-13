using Microsoft.AspNetCore.Mvc;
using Refacto.DotNet.Controllers.Dtos.Product;
using Refacto.DotNet.Controllers.Services;

namespace Refacto.DotNet.Controllers.Controllers
{
    [ApiController]
    [Route("orders")]
    public class OrdersController : ControllerBase
    {
        private readonly IOrderService _orderService;

        public OrdersController(IOrderService orderService)
        {
            _orderService = orderService;
        }

        /// <summary>
        /// Processes the specified order and returns the result.
        /// </summary>
        /// <remarks>This method attempts to process the order with the specified <paramref
        /// name="orderId"/>. If the order is not found, a 404 Not Found response is returned. Otherwise, the processed
        /// order details are returned in the response.</remarks>
        /// <param name="orderId">The unique identifier of the order to process.</param>
        /// <param name="ct">A <see cref="CancellationToken"/> that can be used to cancel the operation.</param>
        /// <returns>An <see cref="ActionResult{T}"/> containing a <see cref="ProcessOrderResponse"/> if the order is
        /// successfully processed; otherwise, a 404 Not Found response if the order does not exist.</returns>
        [HttpPost("{orderId:long}/processOrder")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<ProcessOrderResponse>> ProcessOrder(long orderId, CancellationToken ct)
        {
            ProcessOrderResponse? result = await _orderService.ProcessOrderAsync(orderId, ct);
            if (result is null)
            {
                return NotFound();
            }
            return Ok(result);
        }
    }
}
