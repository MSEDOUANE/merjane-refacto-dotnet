using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Refacto.DotNet.Controllers.Database.Context;
using Refacto.DotNet.Controllers.Dtos.Product;
using Refacto.DotNet.Controllers.Entities;
using Refacto.DotNet.Controllers.Services;
using Refacto.DotNet.Controllers.Services.Impl;

namespace Refacto.DotNet.Controllers.Tests.Services
{
    public class OrderServiceTests
    {
        private static AppDbContext CreateContext()
        {
            DbContextOptions<AppDbContext> options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase($"OrderServiceTests-{Guid.NewGuid()}")
                .Options;

            return new AppDbContext(options);
        }

        private static (Mock<IProductTypeHandler> normal, Mock<IProductTypeHandler> seasonal, Mock<IProductTypeHandler> expirable) CreateHandlerMocks()
        {
            var normal = new Mock<IProductTypeHandler>();
            normal.Setup(h => h.CanHandle(It.Is<Product>(p => string.Equals(p.Type, "NORMAL", StringComparison.OrdinalIgnoreCase))))
                  .Returns(true);
            normal.Setup(h => h.HandleAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()))
                  .Returns(Task.CompletedTask);

            var seasonal = new Mock<IProductTypeHandler>();
            seasonal.Setup(h => h.CanHandle(It.Is<Product>(p => string.Equals(p.Type, "SEASONAL", StringComparison.OrdinalIgnoreCase))))
                    .Returns(true);
            seasonal.Setup(h => h.HandleAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()))
                    .Returns(Task.CompletedTask);

            var expirable = new Mock<IProductTypeHandler>();
            expirable.Setup(h => h.CanHandle(It.Is<Product>(p => string.Equals(p.Type, "EXPIRABLE", StringComparison.OrdinalIgnoreCase))))
                     .Returns(true);
            expirable.Setup(h => h.HandleAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()))
                     .Returns(Task.CompletedTask);

            return (normal, seasonal, expirable);
        }

        [Fact]
        public async Task ProcessOrderAsync_WhenOrderDoesNotExist_ShouldReturnNull_AndNotInvokeHandlers()
        {
            using AppDbContext ctx = CreateContext();
            var (normal, seasonal, expirable) = CreateHandlerMocks();

            IOrderService sut = new OrderService(ctx, new[] { normal.Object, seasonal.Object, expirable.Object });

            ProcessOrderResponse? result = await sut.ProcessOrderAsync(999);

            result.Should().BeNull();
            normal.Verify(h => h.HandleAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()), Times.Never());
            seasonal.Verify(h => h.HandleAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()), Times.Never());
            expirable.Verify(h => h.HandleAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()), Times.Never());
        }

        [Fact]
        public async Task ProcessOrderAsync_WithMixedItems_ShouldInvokeMatchingHandlers_AndReturnResponse()
        {
            using AppDbContext ctx = CreateContext();
            var (normal, seasonal, expirable) = CreateHandlerMocks();

            Product pNormal = new() { Type = "NORMAL", Name = "USB Cable" };
            Product pSeasonal = new() { Type = "SEASONAL", Name = "Watermelon" };
            Product pExpirable = new() { Type = "EXPIRABLE", Name = "Milk" };
            Product pUnknown = new() { Type = "UNKNOWN", Name = "Mystery" };

            await ctx.Products.AddRangeAsync(pNormal, pSeasonal, pExpirable, pUnknown);
            Order order = new()
            {
                Items = new HashSet<Product> { pNormal, pSeasonal, pExpirable, pUnknown }
            };
            await ctx.Orders.AddAsync(order);
            await ctx.SaveChangesAsync();

            IOrderService sut = new OrderService(ctx, [ normal.Object, seasonal.Object, expirable.Object ]);

            ProcessOrderResponse? result = await sut.ProcessOrderAsync(order.Id);

            result.Should().NotBeNull();
            result!.id.Should().Be(order.Id);

            normal.Verify(h => h.HandleAsync(It.Is<Product>(p => p.Id == pNormal.Id), It.IsAny<CancellationToken>()), Times.Once());
            seasonal.Verify(h => h.HandleAsync(It.Is<Product>(p => p.Id == pSeasonal.Id), It.IsAny<CancellationToken>()), Times.Once());
            expirable.Verify(h => h.HandleAsync(It.Is<Product>(p => p.Id == pExpirable.Id), It.IsAny<CancellationToken>()), Times.Once());

            // Unknown type should not be handled by any handler
            normal.Verify(h => h.HandleAsync(It.Is<Product>(p => p.Id == pUnknown.Id), It.IsAny<CancellationToken>()), Times.Never());
            seasonal.Verify(h => h.HandleAsync(It.Is<Product>(p => p.Id == pUnknown.Id), It.IsAny<CancellationToken>()), Times.Never());
            expirable.Verify(h => h.HandleAsync(It.Is<Product>(p => p.Id == pUnknown.Id), It.IsAny<CancellationToken>()), Times.Never());
        }

        [Fact]
        public async Task ProcessOrderAsync_WhenNoItems_ShouldReturnResponse_AndNotInvokeHandlers()
        {
            using AppDbContext ctx = CreateContext();
            var (normal, seasonal, expirable) = CreateHandlerMocks();

            Order order = new() { Items = new HashSet<Product>() };
            await ctx.Orders.AddAsync(order);
            await ctx.SaveChangesAsync();

            IOrderService sut = new OrderService(ctx, new[] { normal.Object, seasonal.Object, expirable.Object });

            ProcessOrderResponse? result = await sut.ProcessOrderAsync(order.Id);

            result.Should().NotBeNull();
            result!.id.Should().Be(order.Id);

            normal.Verify(h => h.HandleAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()), Times.Never());
            seasonal.Verify(h => h.HandleAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()), Times.Never());
            expirable.Verify(h => h.HandleAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()), Times.Never());
        }
    }
}