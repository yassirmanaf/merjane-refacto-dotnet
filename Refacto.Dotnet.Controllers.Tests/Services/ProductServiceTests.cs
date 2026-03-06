using Microsoft.EntityFrameworkCore;
using Moq;
using Refacto.DotNet.Controllers.Database.Context;
using Refacto.DotNet.Controllers.Entities;
using Refacto.DotNet.Controllers.Services;
using Refacto.DotNet.Controllers.Services.Impl;

namespace Refacto.DotNet.Controllers.Tests.Services
{
    public class ProductServiceTests
    {
        // Au départ le test utilisait un DbContext mocké, mais le service utilise maintenant
        // des fonctionnalités EF Core comme Entry(). Ces comportements sont difficiles à
        // simuler correctement avec des mocks. On utilise donc le provider InMemory pour
        // garder des tests simples tout en testant le vrai comportement d’EF Core.
        private readonly Mock<INotificationService> _mockNotificationService;
        private readonly AppDbContext _dbContext;
        private readonly ProductService _productService;

        public ProductServiceTests()
        {
            _mockNotificationService = new Mock<INotificationService>();
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _dbContext = new AppDbContext(options);
            _productService = new ProductService(_mockNotificationService.Object, _dbContext);
        }

        [Fact]
        public void NotifyDelay_Should_SaveChanges_And_SendDelayNotification()
        {
            // GIVEN
            Product product = new()
            {
                LeadTime = 15,
                Available = 0,
                Type = "NORMAL",
                Name = "RJ45 Cable"
            };

            _dbContext.Products.Add(product);
            _dbContext.SaveChanges();

            // WHEN
            _productService.NotifyDelay(product.LeadTime, product);

            // THEN
            Assert.Equal(0, product.Available);
            Assert.Equal(15, product.LeadTime);

            _mockNotificationService.Verify(
                service => service.SendDelayNotification(product.LeadTime, product.Name),
                Times.Once());
        }

        [Fact]
        public void ProcessProduct_Should_DecreaseAvailable_When_ProductIsNormal_And_InStock()
        {
            // GIVEN
            Product product = new()
            {
                Name = "RJ45 Cable",
                Type = "NORMAL",
                Available = 3,
                LeadTime = 15
            };

            _dbContext.Products.Add(product);
            _dbContext.SaveChanges();

            // WHEN
            _productService.ProcessProduct(product);

            // THEN
            Assert.Equal(2, product.Available);

            _mockNotificationService.Verify(
                service => service.SendDelayNotification(It.IsAny<int>(), It.IsAny<string>()),
                Times.Never());
        }

        [Fact]
        public void ProcessProduct_Should_SendOutOfStockNotification_When_ProductIsSeasonal_And_LeadTimeExceedsSeasonEnd()
        {
            // GIVEN
            Product product = new()
            {
                Name = "Summer Drink",
                Type = "SEASONAL",
                Available = 0,
                LeadTime = 10,
                SeasonStartDate = DateTime.Now.AddDays(-5),
                SeasonEndDate = DateTime.Now.AddDays(2)
            };

            _dbContext.Products.Add(product);
            _dbContext.SaveChanges();

            // WHEN
            _productService.ProcessProduct(product);

            // THEN
            Assert.Equal(0, product.Available);

            _mockNotificationService.Verify(
                service => service.SendOutOfStockNotification(product.Name),
                Times.Once());
        }

        [Fact]
        public void ProcessProduct_Should_SendExpirationNotification_When_ProductIsExpirable_And_Expired()
        {
            // GIVEN
            Product product = new()
            {
                Name = "Fresh Milk",
                Type = "EXPIRABLE",
                Available = 3,
                ExpiryDate = DateTime.Now.AddDays(-1)
            };

            _dbContext.Products.Add(product);
            _dbContext.SaveChanges();

            // WHEN
            _productService.ProcessProduct(product);

            // THEN
            Assert.Equal(0, product.Available);

            _mockNotificationService.Verify(
                service => service.SendExpirationNotification(product.Name, (DateTime)product.ExpiryDate),
                Times.Once());
        }
    }
}