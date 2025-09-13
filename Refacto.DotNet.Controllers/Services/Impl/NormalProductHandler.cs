using Refacto.DotNet.Controllers.Entities;
using Refacto.DotNet.Controllers.Services;

namespace Refacto.DotNet.Controllers.Services.Impl
{
    public class NormalProductHandler : IProductTypeHandler
    {
        private readonly IProductService _productService;

        public NormalProductHandler(IProductService productService)
        {
            _productService = productService;
        }

        public bool CanHandle(Product p) =>
            string.Equals(p.Type, "NORMAL", StringComparison.OrdinalIgnoreCase);

        public Task HandleAsync(Product p, CancellationToken ct = default) =>
            _productService.HandleNormalProductAsync(p, ct);
    }
}