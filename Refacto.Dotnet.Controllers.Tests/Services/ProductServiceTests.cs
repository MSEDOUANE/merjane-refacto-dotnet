using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Moq.EntityFrameworkCore;
using Refacto.DotNet.Controllers.Database.Context;
using Refacto.DotNet.Controllers.Entities;
using Refacto.DotNet.Controllers.Services;
using Refacto.DotNet.Controllers.Services.Impl;

namespace Refacto.DotNet.Controllers.Tests.Services
{
    public class ProductServiceTests
    {
        private readonly Mock<INotificationService> _mockNotificationService;
        private readonly Mock<AppDbContext> _mockDbContext;
        private readonly Mock<DbSet<Product>> _mockDbSet;
        private readonly ProductService _productService;

        public ProductServiceTests()
        {
            _mockNotificationService = new Mock<INotificationService>();
            _mockDbContext = new Mock<AppDbContext>();
            _mockDbSet = new Mock<DbSet<Product>>();
            _ = _mockDbContext.Setup(x => x.Products).ReturnsDbSet(Array.Empty<Product>());
            _productService = new ProductService(_mockNotificationService.Object, _mockDbContext.Object);
        }

        [Fact]
        public async Task NotifyDelay_Should_SetLeadTime_Save_And_Notify()
        {
            // GIVEN
            Product product = new()
            {
                LeadTime = 15,
                Available = 0,
                Type = "NORMAL",
                Name = "RJ45 Cable"
            };

            // WHEN
            await _productService.NotifyDelayAsync(product.LeadTime, product);

            // THEN
            product.Available.Should().Be(0);
            product.LeadTime.Should().Be(15);
            _mockDbContext.Verify(ctx => ctx.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once());
            _mockNotificationService.Verify(s => s.SendDelayNotification(product.LeadTime, product.Name!), Times.Once());
        }

        [Fact]
        public async Task HandleNormalProduct_WhenInStock_Should_DecrementAndSave()
        {
            // GIVEN
            Product p = new()
            {
                Type = "NORMAL",
                Available = 3,
                LeadTime = 5,
                Name = "USB Cable"
            };

            // WHEN
            await _productService.HandleNormalProductAsync(p);

            // THEN
            p.Available.Should().Be(2);
            _mockDbContext.Verify(ctx => ctx.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once());
            _mockNotificationService.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task HandleNormalProduct_WhenOutOfStock_WithLeadTime_Should_NotifyDelay()
        {
            // GIVEN
            Product p = new()
            {
                Type = "NORMAL",
                Available = 0,
                LeadTime = 7,
                Name = "USB Dongle"
            };

            // WHEN
            await _productService.HandleNormalProductAsync(p);

            // THEN
            p.Available.Should().Be(0);
            _mockDbContext.Verify(ctx => ctx.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once());
            _mockNotificationService.Verify(s => s.SendDelayNotification(7, "USB Dongle"), Times.Once());
        }

        [Fact]
        public async Task HandleSeasonalProduct_InSeason_WithStock_Should_DecrementAndSave()
        {
            // GIVEN
            DateTime now = DateTime.Now;
            Product p = new()
            {
                Type = "SEASONAL",
                Available = 10,
                LeadTime = 5,
                Name = "Watermelon",
                SeasonStartDate = now.AddDays(-2),
                SeasonEndDate = now.AddDays(30)
            };

            // WHEN
            await _productService.HandleSeasonalProductAsync(p);

            // THEN
            p.Available.Should().Be(9);
            _mockDbContext.Verify(ctx => ctx.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once());
            _mockNotificationService.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task HandleSeasonalProduct_LeadTimeExceedsSeasonEnd_Should_NotifyOutOfStock_AndZeroAvailability()
        {
            // GIVEN
            DateTime now = DateTime.Now;
            Product p = new()
            {
                Type = "SEASONAL",
                Available = 0,
                LeadTime = 20,
                Name = "Mango",
                SeasonStartDate = now.AddDays(-5),
                SeasonEndDate = now.AddDays(10) // now + 20 > end -> OOS
            };

            // WHEN
            await _productService.HandleSeasonalProductAsync(p);

            // THEN
            p.Available.Should().Be(0);
            _mockNotificationService.Verify(s => s.SendOutOfStockNotification("Mango"), Times.Once());
            _mockDbContext.Verify(ctx => ctx.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once());
        }

        [Fact]
        public async Task HandleSeasonalProduct_BeforeSeasonStart_Should_NotifyOutOfStock()
        {
            // GIVEN
            DateTime now = DateTime.Now;
            Product p = new()
            {
                Type = "SEASONAL",
                Available = 0,
                LeadTime = 3,
                Name = "Grapes",
                SeasonStartDate = now.AddDays(10),
                SeasonEndDate = now.AddDays(40)
            };

            // WHEN
            await _productService.HandleSeasonalProductAsync(p);

            // THEN
            _mockNotificationService.Verify(s => s.SendOutOfStockNotification("Grapes"), Times.Once());
            _mockDbContext.Verify(ctx => ctx.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once());
        }

        [Fact]
        public async Task HandleSeasonalProduct_InSeason_OutOfStock_WithAcceptableLeadTime_Should_NotifyDelay()
        {
            // GIVEN
            DateTime now = DateTime.Now;
            Product p = new()
            {
                Type = "SEASONAL",
                Available = 0,
                LeadTime = 5,
                Name = "Peach",
                SeasonStartDate = now.AddDays(-2),
                SeasonEndDate = now.AddDays(10) // now + 5 <= end -> delay
            };

            // WHEN
            await _productService.HandleSeasonalProductAsync(p);

            // THEN
            _mockNotificationService.Verify(s => s.SendDelayNotification(5, "Peach"), Times.Once());
            _mockDbContext.Verify(ctx => ctx.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once());
        }

        [Fact]
        public async Task HandleExpiredProduct_NotExpired_WithStock_Should_DecrementAndSave()
        {
            // GIVEN
            Product p = new()
            {
                Type = "EXPIRABLE",
                Available = 4,
                LeadTime = 0,
                Name = "Butter",
                ExpiryDate = DateTime.Now.AddDays(2)
            };

            // WHEN
            await _productService.HandleExpiredProductAsync(p);

            // THEN
            p.Available.Should().Be(3);
            _mockDbContext.Verify(ctx => ctx.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once());
            _mockNotificationService.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task HandleExpiredProduct_ExpiredOrNoStock_Should_NotifyExpiration_AndZeroAvailability()
        {
            // GIVEN
            Product p = new()
            {
                Type = "EXPIRABLE",
                Available = 0, 
                LeadTime = 0,
                Name = "Milk",
                ExpiryDate = DateTime.Now.AddDays(-1)
            };

            // WHEN
            await _productService.HandleExpiredProductAsync(p);

            // THEN
            p.Available.Should().Be(0);
            _mockNotificationService.Verify(s => s.SendExpirationNotification("Milk", p.ExpiryDate!.Value), Times.Once());
            _mockDbContext.Verify(ctx => ctx.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once());
        }


        [Fact]
        public async Task HandleExpiredProduct_WhenExpiredWithStock_Should_NotifyExpiration_AndSetAvailabilityToZero()
        {
            // GIVEN
            Product p = new()
            {
                Type = "EXPIRABLE",
                Available = 5,                 // has stock
                LeadTime = 0,
                Name = "Yogurt",
                ExpiryDate = DateTime.Now.AddMinutes(-10) // expired
            };

            // WHEN
            await _productService.HandleExpiredProductAsync(p);

            // THEN
            p.Available.Should().Be(0, "expired items should not be sold and stock is zeroed");
            _mockNotificationService.Verify(s => s.SendExpirationNotification("Yogurt", p.ExpiryDate!.Value), Times.Once());
            _mockNotificationService.VerifyNoOtherCalls();
            _mockDbContext.Verify(ctx => ctx.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once());
        }
    }
}
