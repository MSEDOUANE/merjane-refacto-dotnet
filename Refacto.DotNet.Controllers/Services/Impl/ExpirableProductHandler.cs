using Refacto.DotNet.Controllers.Entities;
using Refacto.DotNet.Controllers.Services;

namespace Refacto.DotNet.Controllers.Services.Impl
{
    public class ExpirableProductHandler : IProductTypeHandler
    {
        private readonly IProductService _productService;

        public ExpirableProductHandler(IProductService productService)
        {
            _productService = productService;
        }

        public bool CanHandle(Product p) =>
            string.Equals(p.Type, "EXPIRABLE", StringComparison.OrdinalIgnoreCase);

        public Task HandleAsync(Product p, CancellationToken ct = default) =>
            _productService.HandleExpiredProductAsync(p, ct);
    }
}