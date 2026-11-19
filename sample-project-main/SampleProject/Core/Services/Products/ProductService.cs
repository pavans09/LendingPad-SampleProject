using System;
using System.Collections.Generic;
using BusinessEntities;
using Common;
using Core.Factories;
using Data.Repositories;

namespace Core.Services.Products
{
    [AutoRegister]
    public class ProductService : IProductService
    {
        private readonly IProductRepository _repository;
        private readonly IIdObjectFactory<Product> _factory;

        public ProductService(IProductRepository repository, IIdObjectFactory<Product> factory)
        {
            _repository = repository;
            _factory = factory;
        }

        public Product Create(Guid id, string name, string description, decimal price, int stockQuantity)
        {
            var product = _factory.Create(id);
            SetProperties(product, name, description, price, stockQuantity);
            _repository.Save(product);
            return product;
        }

        public Product Update(Guid id, string name, string description, decimal price, int stockQuantity)
        {
            var product = _repository.Get(id);
            if (product == null)
            {
                return null;
            }

            SetProperties(product, name, description, price, stockQuantity);
            _repository.Save(product);
            return product;
        }

        public void Delete(Guid id)
        {
            _repository.Delete(id);
        }

        public Product Get(Guid id)
        {
            return _repository.Get(id);
        }

        public IEnumerable<Product> GetList(string name = null, decimal? minPrice = null, decimal? maxPrice = null, bool? inStock = null)
        {
            return _repository.Get(name, minPrice, maxPrice, inStock);
        }

        private static void SetProperties(Product product, string name, string description, decimal price, int stockQuantity)
        {
            product.SetName(name);
            product.SetDescription(description);
            product.SetPrice(price);
            product.SetStockQuantity(stockQuantity);
        }
    }
}
