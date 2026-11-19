using System;
using System.Collections.Generic;
using BusinessEntities;

namespace Data.Repositories
{
    public interface IProductRepository
    {
        void Save(Product product);
        void Delete(Guid id);
        Product Get(Guid id);
        IEnumerable<Product> Get(string name = null, decimal? minPrice = null, decimal? maxPrice = null, bool? inStock = null);
    }
}
