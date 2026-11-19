using System;
using System.Collections.Generic;
using BusinessEntities;

namespace Core.Services.Products
{
    public interface IProductService
    {
        Product Create(Guid id, string name, string description, decimal price, int stockQuantity);
        Product Update(Guid id, string name, string description, decimal price, int stockQuantity);
        void Delete(Guid id);
        Product Get(Guid id);
        IEnumerable<Product> GetList(string name = null, decimal? minPrice = null, decimal? maxPrice = null, bool? inStock = null);
    }
}
