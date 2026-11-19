using System;

namespace BusinessEntities
{
    public class Product : IdObject
    {
        public Product()
        {
        }

        public Product(Guid id) : base(id)
        {
        }

        public string Name { get; private set; }
        public string Description { get; private set; }
        public decimal Price { get; private set; }
        public int StockQuantity { get; private set; }

        public void SetName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Product name is required.", nameof(name));
            }

            Name = name;
        }

        public void SetDescription(string description)
        {
            Description = description;
        }

        public void SetPrice(decimal price)
        {
            if (price < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(price), "Product price cannot be negative.");
            }

            Price = price;
        }

        public void SetStockQuantity(int stockQuantity)
        {
            if (stockQuantity < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(stockQuantity), "Stock quantity cannot be negative.");
            }

            StockQuantity = stockQuantity;
        }
    }
}
