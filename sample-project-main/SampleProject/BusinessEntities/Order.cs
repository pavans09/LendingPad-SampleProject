using System;

namespace BusinessEntities
{
    public class Order : IdObject
    {
        public Order()
        {
        }

        public Order(Guid id) : base(id)
        {
        }

        public string CustomerName { get; private set; }
        public DateTime OrderDate { get; private set; }
        public string Status { get; private set; }
        public Guid ProductId { get; private set; }
        public int Quantity { get; private set; }
        public decimal TotalAmount { get; private set; }

        public void SetCustomerName(string customerName)
        {
            if (string.IsNullOrWhiteSpace(customerName))
            {
                throw new ArgumentException("Customer name is required.", nameof(customerName));
            }

            CustomerName = customerName;
        }

        public void SetOrderDate(DateTime orderDate)
        {
            OrderDate = orderDate.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(orderDate, DateTimeKind.Utc)
                : orderDate.ToUniversalTime();
        }

        public void SetStatus(string status)
        {
            if (string.IsNullOrWhiteSpace(status))
            {
                throw new ArgumentException("Order status is required.", nameof(status));
            }

            Status = status;
        }

        public void SetProduct(Guid productId, int quantity)
        {
            if (productId == Guid.Empty)
            {
                throw new ArgumentException("A product is required for an order.", nameof(productId));
            }

            if (quantity <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(quantity), "Order quantity must be greater than zero.");
            }

            ProductId = productId;
            Quantity = quantity;
        }

        public void SetTotalAmount(decimal totalAmount)
        {
            if (totalAmount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(totalAmount), "Order total cannot be negative.");
            }

            TotalAmount = totalAmount;
        }
    }
}
