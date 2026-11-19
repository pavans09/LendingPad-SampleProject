using System;
using BusinessEntities;
using WebApi.Models;

namespace WebApi.Models.Orders
{
    public class OrderData : IdObjectData
    {
        public OrderData(Order order) : base(order)
        {
            CustomerName = order.CustomerName;
            OrderDate = order.OrderDate;
            Status = order.Status;
            ProductId = order.ProductId;
            Quantity = order.Quantity;
            TotalAmount = order.TotalAmount;
        }

        public string CustomerName { get; set; }
        public DateTime OrderDate { get; set; }
        public string Status { get; set; }
        public Guid ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal TotalAmount { get; set; }
    }
}
