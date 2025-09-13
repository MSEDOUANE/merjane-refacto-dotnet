using Refacto.DotNet.Controllers.Entities;
using Refacto.DotNet.Controllers.Services;

namespace Refacto.DotNet.Controllers.Services.Impl
{
    public class SeasonalProductHandler : IProductTypeHandler
    {
        private readonly IProductService _productService;

        public SeasonalProductHandler(IProductService productService)
        {
            _productService = productService;
        }

        public bool CanHandle(Product p) =>
            string.Equals(p.Type, "SEASONAL", StringComparison.OrdinalIgnoreCase);

        public Task HandleAsync(Product p, CancellationToken ct = default) =>
            _productService.HandleSeasonalProductAsync(p, ct);
    }
}