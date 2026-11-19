using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using BusinessEntities;

namespace Data.Repositories
{
    public class ProductRepository : IProductRepository
    {
        private readonly string _connectionString;
        private readonly DbProviderFactory _providerFactory;

        public ProductRepository(string connectionString, DbProviderFactory providerFactory)
        {
            _connectionString = connectionString;
            _providerFactory = providerFactory;
        }

        public void Save(Product product)
        {
            using (var connection = CreateConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"
                    IF EXISTS (SELECT 1 FROM dbo.Products WHERE ProductId = @id)
                        UPDATE dbo.Products SET Name = @name, Description = @description,
                            Price = @price, StockQuantity = @stockQuantity WHERE ProductId = @id;
                    ELSE
                        INSERT INTO dbo.Products (ProductId, Name, Description, Price, StockQuantity)
                        VALUES (@id, @name, @description, @price, @stockQuantity);";
                AddParameter(command, "@id", DbType.Guid, product.Id);
                AddParameter(command, "@name", DbType.String, product.Name, 200);
                AddParameter(command, "@description", DbType.String, (object)product.Description ?? DBNull.Value, -1);
                var priceParameter = AddParameter(command, "@price", DbType.Decimal, product.Price);
                AddParameter(command, "@stockQuantity", DbType.Int32, product.StockQuantity);
                connection.Open();
                command.ExecuteNonQuery();
            }
        }

        public void Delete(Guid id)
        {
            using (var connection = CreateConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "DELETE FROM dbo.Products WHERE ProductId = @id;";
                AddParameter(command, "@id", DbType.Guid, id);
                connection.Open();
                command.ExecuteNonQuery();
            }
        }

        public Product Get(Guid id)
        {
            using (var connection = CreateConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT ProductId, Name, Description, Price, StockQuantity FROM dbo.Products WHERE ProductId = @id;";
                AddParameter(command, "@id", DbType.Guid, id);
                connection.Open();
                using (var reader = command.ExecuteReader())
                {
                    if (!reader.Read()) return null;

                    var product = new Product(reader.GetGuid(0));
                    product.SetName(reader.GetString(1));
                    product.SetDescription(reader.IsDBNull(2) ? null : reader.GetString(2));
                    product.SetPrice(reader.GetDecimal(3));
                    product.SetStockQuantity(reader.GetInt32(4));
                    return product;
                }
            }
        }

        public IEnumerable<Product> Get(string name = null, decimal? minPrice = null, decimal? maxPrice = null, bool? inStock = null)
        {
            var filters = new List<string>();
            if (name != null) filters.Add("Name LIKE @name");
            if (minPrice.HasValue) filters.Add("Price >= @minPrice");
            if (maxPrice.HasValue) filters.Add("Price <= @maxPrice");
            if (inStock.HasValue) filters.Add(inStock.Value ? "StockQuantity > 0" : "StockQuantity = 0");

            var query = "SELECT ProductId, Name, Description, Price, StockQuantity FROM dbo.Products";
            if (filters.Count > 0) query += " WHERE " + string.Join(" AND ", filters);
            query += " ORDER BY Name, ProductId;";

            using (var connection = CreateConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = query;
                if (name != null) AddParameter(command, "@name", DbType.String, "%" + name + "%", -1);
                if (minPrice.HasValue) AddParameter(command, "@minPrice", DbType.Decimal, minPrice.Value);
                if (maxPrice.HasValue) AddParameter(command, "@maxPrice", DbType.Decimal, maxPrice.Value);
                connection.Open();
                using (var reader = command.ExecuteReader())
                {
                    var products = new List<Product>();
                    while (reader.Read())
                    {
                        var product = new Product(reader.GetGuid(0));
                        product.SetName(reader.GetString(1));
                        product.SetDescription(reader.IsDBNull(2) ? null : reader.GetString(2));
                        product.SetPrice(reader.GetDecimal(3));
                        product.SetStockQuantity(reader.GetInt32(4));
                        products.Add(product);
                    }

                    return products;
                }
            }
        }

        private DbConnection CreateConnection()
        {
            var connection = _providerFactory.CreateConnection();
            if (connection == null)
            {
                throw new InvalidOperationException("The configured SQL Server provider could not create a connection.");
            }

            connection.ConnectionString = _connectionString;
            return connection;
        }

        private static DbParameter AddParameter(DbCommand command, string name, DbType type, object value, int? size = null)
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = name;
            parameter.DbType = type;
            if (size.HasValue) parameter.Size = size.Value;
            parameter.Value = value;
            command.Parameters.Add(parameter);
            return parameter;
        }
    }
}
