using Microsoft.EntityFrameworkCore;
using Refacto.DotNet.Controllers.Database.Context;
using Refacto.DotNet.Controllers.Entities;
using Refacto.DotNet.Services;

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

        public void NotifyDelay(int leadTime, Product p)
        {
            p.LeadTime = leadTime;
            _ = _ctx.SaveChanges();
            _ns.SendDelayNotification(leadTime, p.Name);
        }

        public void ProcessProduct(Product p)
        {
            if (p.Type == "NORMAL")
                {
                    if (p.Available > 0)
                    {
                        p.Available -= 1;
                        _ctx.Entry(p).State = EntityState.Modified;
                        _ = _ctx.SaveChanges();
                    }
                    else
                    {
                        int leadTime = p.LeadTime;
                        if (leadTime > 0)
                        {
                            NotifyDelay(leadTime, p);
                        }
                    }
                }
                else if (p.Type == "SEASONAL")
                {
                    if (DateTime.Now.Date > p.SeasonStartDate && DateTime.Now.Date < p.SeasonEndDate && p.Available > 0)
                    {
                        p.Available -= 1;
                        _ = _ctx.SaveChanges();
                    }
                    else
                    {
                        HandleSeasonalProduct(p);
                    }
                }
                else if (p.Type == "EXPIRABLE")
                {
                    if (p.Available > 0 && p.ExpiryDate > DateTime.Now.Date)
                    {
                        p.Available -= 1;
                        _ = _ctx.SaveChanges();
                    }
                    else
                    {
                        HandleExpiredProduct(p);
                    }
                }
        }

        private void HandleSeasonalProduct(Product p)
        {
            if (DateTime.Now.AddDays(p.LeadTime) > p.SeasonEndDate)
            {
                _ns.SendOutOfStockNotification(p.Name);
                p.Available = 0;
                _ = _ctx.SaveChanges();
            }
            else if (p.SeasonStartDate > DateTime.Now)
            {
                _ns.SendOutOfStockNotification(p.Name);
                _ = _ctx.SaveChanges();
            }
            else
            {
                NotifyDelay(p.LeadTime, p);
            }
        }

        private void HandleExpiredProduct(Product p)
        {
            if (p.Available > 0 && p.ExpiryDate > DateTime.Now)
            {
                p.Available -= 1;
                _ = _ctx.SaveChanges();
            }
            else
            {
                _ns.SendExpirationNotification(p.Name, (DateTime)p.ExpiryDate);
                p.Available = 0;
                _ = _ctx.SaveChanges();
            }
        }
    }
}