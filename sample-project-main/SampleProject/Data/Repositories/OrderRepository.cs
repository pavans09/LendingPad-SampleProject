using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using BusinessEntities;

namespace Data.Repositories
{
    public class OrderRepository : IOrderRepository
    {
        private readonly string _connectionString;
        private readonly DbProviderFactory _providerFactory;

        public OrderRepository(string connectionString, DbProviderFactory providerFactory)
        {
            _connectionString = connectionString;
            _providerFactory = providerFactory;
        }

        public void Save(Order order)
        {
            using (var connection = CreateConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"
                    IF EXISTS (SELECT 1 FROM dbo.Orders WHERE OrderId = @id)
                            UPDATE dbo.Orders SET CustomerName = @customerName, OrderDate = @orderDate,
                                Status = @status, ProductId = @productId, Quantity = @quantity,
                                TotalAmount = @totalAmount WHERE OrderId = @id;
                        ELSE
                            INSERT INTO dbo.Orders (OrderId, CustomerName, OrderDate, Status, ProductId, Quantity, TotalAmount)
                            VALUES (@id, @customerName, @orderDate, @status, @productId, @quantity, @totalAmount);";
                AddParameter(command, "@id", DbType.Guid, order.Id);
                AddParameter(command, "@customerName", DbType.String, order.CustomerName, 200);
                AddParameter(command, "@orderDate", DbType.DateTime2, order.OrderDate);
                AddParameter(command, "@status", DbType.String, order.Status, 50);
                AddParameter(command, "@productId", DbType.Guid, order.ProductId);
                AddParameter(command, "@quantity", DbType.Int32, order.Quantity);
                var amountParameter = AddParameter(command, "@totalAmount", DbType.Decimal, order.TotalAmount);
                amountParameter.Precision = 19;
                amountParameter.Scale = 4;
                connection.Open();
                command.ExecuteNonQuery();
            }
        }

        public void Delete(Guid id)
        {
            using (var connection = CreateConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "DELETE FROM dbo.Orders WHERE OrderId = @id;";
                AddParameter(command, "@id", DbType.Guid, id);
                connection.Open();
                command.ExecuteNonQuery();
            }
        }

        public Order Get(Guid id)
        {
            using (var connection = CreateConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT OrderId, CustomerName, OrderDate, Status, ProductId, Quantity, TotalAmount FROM dbo.Orders WHERE OrderId = @id;";
                AddParameter(command, "@id", DbType.Guid, id);
                connection.Open();
                using (var reader = command.ExecuteReader())
                {
                    if (!reader.Read()) return null;

                    var order = new Order(reader.GetGuid(0));
                    order.SetCustomerName(reader.GetString(1));
                    order.SetOrderDate(reader.GetDateTime(2));
                    order.SetStatus(reader.GetString(3));
                    if (!reader.IsDBNull(4)) order.SetProduct(reader.GetGuid(4), reader.GetInt32(5));
                    order.SetTotalAmount(reader.GetDecimal(6));
                    return order;
                }
            }
        }

        public IEnumerable<Order> Get(string customerName = null, string status = null, DateTime? fromDate = null, DateTime? toDate = null, Guid? productId = null)
        {
            var filters = new List<string>();
            if (customerName != null) filters.Add("CustomerName LIKE @customerName");
            if (status != null) filters.Add("Status = @status");
            if (fromDate.HasValue) filters.Add("OrderDate >= @fromDate");
            if (toDate.HasValue) filters.Add("OrderDate <= @toDate");
            if (productId.HasValue) filters.Add("ProductId = @productId");

            var query = "SELECT OrderId, CustomerName, OrderDate, Status, ProductId, Quantity, TotalAmount FROM dbo.Orders";
            if (filters.Count > 0) query += " WHERE " + string.Join(" AND ", filters);
            query += " ORDER BY OrderDate DESC, OrderId;";

            using (var connection = CreateConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = query;
                if (customerName != null) AddParameter(command, "@customerName", DbType.String, "%" + customerName + "%", -1);
                if (status != null) AddParameter(command, "@status", DbType.String, status, 50);
                if (fromDate.HasValue) AddParameter(command, "@fromDate", DbType.DateTime2, fromDate.Value);
                if (toDate.HasValue) AddParameter(command, "@toDate", DbType.DateTime2, toDate.Value);
                if (productId.HasValue) AddParameter(command, "@productId", DbType.Guid, productId.Value);
                connection.Open();
                using (var reader = command.ExecuteReader())
                {
                    var orders = new List<Order>();
                    while (reader.Read())
                    {
                        var order = new Order(reader.GetGuid(0));
                        order.SetCustomerName(reader.GetString(1));
                        order.SetOrderDate(reader.GetDateTime(2));
                        order.SetStatus(reader.GetString(3));
                        if (!reader.IsDBNull(4)) order.SetProduct(reader.GetGuid(4), reader.GetInt32(5));
                        order.SetTotalAmount(reader.GetDecimal(6));
                        orders.Add(order);
                    }

                    return orders;
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
