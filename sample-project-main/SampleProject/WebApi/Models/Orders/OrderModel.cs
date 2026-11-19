using System;

namespace WebApi.Models.Orders
{
    public class OrderModel
    {
        public string CustomerName { get; set; }
        public DateTime? OrderDate { get; set; }
        public string Status { get; set; }
        public Guid ProductId { get; set; }
        public int Quantity { get; set; }
    }
}
