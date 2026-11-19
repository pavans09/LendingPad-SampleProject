using System;
using System.Collections.Generic;
using BusinessEntities;
using Common;
using Core.Factories;
using Data.Repositories;

namespace Core.Services.Orders
{
    [AutoRegister]
    public class OrderService : IOrderService
    {
        private readonly IOrderRepository _repository;
        private readonly IProductRepository _productRepository;
        private readonly IIdObjectFactory<Order> _factory;

        public OrderService(IOrderRepository repository, IProductRepository productRepository, IIdObjectFactory<Order> factory)
        {
            _repository = repository;
            _productRepository = productRepository;
            _factory = factory;
        }

        public Order Create(Guid id, string customerName, DateTime? orderDate, string status, Guid productId, int quantity)
        {
            var product = GetRequiredProduct(productId);
            var order = _factory.Create(id);
            SetProperties(order, customerName, orderDate ?? DateTime.UtcNow, status, product, quantity);
            _repository.Save(order);
            return order;
        }

        public Order Update(Guid id, string customerName, DateTime? orderDate, string status, Guid productId, int quantity)
        {
            var order = _repository.Get(id);
            if (order == null)
            {
                return null;
            }

            var product = GetRequiredProduct(productId);
            SetProperties(order, customerName, orderDate ?? order.OrderDate, status, product, quantity);
            _repository.Save(order);
            return order;
        }

        public void Delete(Guid id)
        {
            _repository.Delete(id);
        }

        public Order Get(Guid id)
        {
            return _repository.Get(id);
        }

        public IEnumerable<Order> GetList(string customerName = null, string status = null, DateTime? fromDate = null, DateTime? toDate = null, Guid? productId = null)
        {
            return _repository.Get(customerName, status, fromDate, toDate, productId);
        }

        private static void SetProperties(Order order, string customerName, DateTime orderDate, string status, Product product, int quantity)
        {
            order.SetCustomerName(customerName);
            order.SetOrderDate(orderDate);
            order.SetStatus(status);
            order.SetProduct(product.Id, quantity);
            order.SetTotalAmount(product.Price * quantity);
        }

        private Product GetRequiredProduct(Guid productId)
        {
            var product = _productRepository.Get(productId);
            if (product == null)
            {
                throw new ArgumentException("The specified product does not exist.", nameof(productId));
            }

            return product;
        }
    }
}
