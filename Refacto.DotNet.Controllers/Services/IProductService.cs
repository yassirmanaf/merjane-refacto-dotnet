
using Refacto.DotNet.Controllers.Entities;

namespace Refacto.DotNet.Services
{
    public interface IProductService
    {
        void NotifyDelay(int leadTime, Product p);
        void ProcessProduct(Product p);        
    }   
}