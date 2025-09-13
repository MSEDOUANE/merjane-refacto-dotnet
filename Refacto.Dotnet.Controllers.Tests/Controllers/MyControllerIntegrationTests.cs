using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Refacto.DotNet.Controllers.Database.Context;
using Refacto.DotNet.Controllers.Entities;
using Refacto.DotNet.Controllers.Services;

namespace Refacto.Dotnet.Controllers.Tests.Controllers
{
    [Collection("Sequential")]
    public class MyControllerIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly AppDbContext _context;
        private readonly Mock<INotificationService> _mockNotificationService;

        public MyControllerIntegrationTests(WebApplicationFactory<Program> factory)
        {
            _mockNotificationService = new Mock<INotificationService>();

            _factory = factory.WithWebHostBuilder(builder =>
            {
                _ = builder.ConfigureServices(services =>
                {
                    _ = services.AddSingleton(_mockNotificationService.Object);

                    ServiceDescriptor? descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                    if (descriptor != null)
                    {
                        _ = services.Remove(descriptor);
                    }

                    // Add ApplicationDbContext using an in-memory database for testing
                    _ = services.AddDbContext<AppDbContext>(options =>
                    {
                        _ = options.UseInMemoryDatabase($"InMemoryDbForTesting-{GetType()}");
                    });
                    _ = services.AddScoped((_sp) => _mockNotificationService.Object);


                    ServiceProvider sp = services.BuildServiceProvider();
                });
            });

            IServiceScope scope = _factory.Services.CreateScope();
            _context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            _context.Database.EnsureDeleted();
            _context.Database.EnsureCreated();
        }

        [Fact]
        public async Task ProcessOrderShouldReturn()
        {
            HttpClient client = _factory.CreateClient();

            List<Product> allProducts = CreateProducts();
            HashSet<Product> orderItems = new(allProducts);
            Order order = CreateOrder(orderItems);
            await _context.Products.AddRangeAsync(allProducts);
            _ = await _context.Orders.AddAsync(order);
            _ = await _context.SaveChangesAsync();
            _context.ChangeTracker.Clear();

            HttpResponseMessage response = await client.PostAsync($"/orders/{order.Id}/processOrder", null);
            _ = response.EnsureSuccessStatusCode();

            Order? resultOrder = await _context.Orders.FindAsync(order.Id);
            Assert.Equal(resultOrder.Id, order.Id);
        }

        [Fact]
        public async Task ProcessOrder_Should_Update_Availability_And_Send_Expected_Notifications()
        {
            HttpClient client = _factory.CreateClient();

            // Arrange
            List<Product> allProducts = CreateProducts();
            HashSet<Product> orderItems = new(allProducts);
            Order order = CreateOrder(orderItems);

            await _context.Products.AddRangeAsync(allProducts);
            _ = await _context.Orders.AddAsync(order);
            _ = await _context.SaveChangesAsync();
            _context.ChangeTracker.Clear();

            // Act
            HttpResponseMessage response = await client.PostAsync($"/orders/{order.Id}/processOrder", null);
            _ = response.EnsureSuccessStatusCode();

            // Assert: order exists
            Order? resultOrder = await _context.Orders.FindAsync(order.Id);
            resultOrder.Should().NotBeNull();
            resultOrder!.Id.Should().Be(order.Id);

            // Reload products from DB and assert availability updates
            List<Product> updated = await _context.Products.ToListAsync();

            updated.Single(p => p.Name == "USB Cable").Available.Should().Be(29);      // NORMAL, in stock => -1
            updated.Single(p => p.Name == "USB Dongle").Available.Should().Be(0);      // NORMAL, OOS => notify delay
            updated.Single(p => p.Name == "Butter").Available.Should().Be(29);         // EXPIRABLE, not expired => -1
            updated.Single(p => p.Name == "Milk").Available.Should().Be(0);            // EXPIRABLE, expired => set 0 + notify
            updated.Single(p => p.Name == "Watermelon").Available.Should().Be(29);     // SEASONAL, in season => -1
            updated.Single(p => p.Name == "Grapes").Available.Should().Be(30);         // SEASONAL, before season => OOS notify, no change

            // Verify expected notifications
            _mockNotificationService.Verify(s => s.SendDelayNotification(10, "USB Dongle"), Times.Once());
            _mockNotificationService.Verify(s => s.SendExpirationNotification("Milk",
                It.Is<DateTime>(d => d.Date == updated.Single(p => p.Name == "Milk").ExpiryDate!.Value.Date)), Times.Once());
            _mockNotificationService.Verify(s => s.SendOutOfStockNotification("Grapes"), Times.Once());
        }

        private static Order CreateOrder(HashSet<Product> products)
        {
            return new Order { Items = products };
        }

        private static List<Product> CreateProducts()
        {
            return new List<Product>
            {
                new Product { LeadTime = 15, Available = 30, Type = "NORMAL", Name = "USB Cable" },
                new Product { LeadTime = 10, Available = 0, Type = "NORMAL", Name = "USB Dongle" },
                new Product { LeadTime = 15, Available = 30, Type = "EXPIRABLE", Name = "Butter", ExpiryDate = DateTime.Now.AddDays(26) },
                new Product { LeadTime = 90, Available = 6, Type = "EXPIRABLE", Name = "Milk", ExpiryDate = DateTime.Now.AddDays(-2) },
                new Product { LeadTime = 15, Available = 30, Type = "SEASONAL", Name = "Watermelon", SeasonStartDate = DateTime.Now.AddDays(-2), SeasonEndDate = DateTime.Now.AddDays(58) },
                new Product { LeadTime = 15, Available = 30, Type = "SEASONAL", Name = "Grapes", SeasonStartDate = DateTime.Now.AddDays(180), SeasonEndDate = DateTime.Now.AddDays(240) }
            };
        }
    }
}
