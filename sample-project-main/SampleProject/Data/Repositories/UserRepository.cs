using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using BusinessEntities;

namespace Data.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly string _connectionString;
        private readonly DbProviderFactory _providerFactory;

        public UserRepository(string connectionString, DbProviderFactory providerFactory)
        {
            _connectionString = connectionString;
            _providerFactory = providerFactory;
        }

        public static void InitializeDatabase(string connectionString, DbProviderFactory providerFactory)
        {
            var builder = new DbConnectionStringBuilder
            {
                ConnectionString = connectionString
            };
            if (!builder.TryGetValue("Initial Catalog", out var databaseNameValue) ||
                string.IsNullOrWhiteSpace(Convert.ToString(databaseNameValue)))
            {
                throw new InvalidOperationException("The user database connection string must specify an initial catalog.");
            }

            var databaseName = Convert.ToString(databaseNameValue);
            builder["Initial Catalog"] = "master";
            using (var connection = CreateConnection(providerFactory, builder.ConnectionString))
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"IF DB_ID(@databaseName) IS NULL
                                        BEGIN
                                            DECLARE @createDatabase nvarchar(258);
                                            SET @createDatabase = N'CREATE DATABASE ' + QUOTENAME(@databaseName);
                                            EXEC(@createDatabase);
                                        END";
                AddParameter(command, "@databaseName", DbType.String, databaseName, 128);
                connection.Open();
                command.ExecuteNonQuery();
            }

            builder["Initial Catalog"] = databaseName;
            using (var connection = CreateConnection(providerFactory, builder.ConnectionString))
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"
                    IF OBJECT_ID(N'dbo.Users', N'U') IS NULL
                    BEGIN
                        CREATE TABLE dbo.Users
                        (
                            Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_Users PRIMARY KEY,
                            Name NVARCHAR(MAX) NOT NULL,
                            Email NVARCHAR(MAX) NOT NULL,
                            Type INT NOT NULL,
                            MonthlySalary DECIMAL(19, 4) NULL,
                            Age INT NOT NULL
                        );
                    END;

                    IF OBJECT_ID(N'dbo.UserTags', N'U') IS NULL
                    BEGIN
                        CREATE TABLE dbo.UserTags
                        (
                            UserId UNIQUEIDENTIFIER NOT NULL,
                            Tag NVARCHAR(450) COLLATE Latin1_General_100_BIN2 NULL,
                            CONSTRAINT FK_UserTags_Users FOREIGN KEY (UserId)
                                REFERENCES dbo.Users(Id) ON DELETE CASCADE
                        );
                    END;

                    IF NOT EXISTS
                    (
                        SELECT 1 FROM sys.indexes
                        WHERE object_id = OBJECT_ID(N'dbo.UserTags')
                          AND name = N'IX_UserTags_Tag'
                    )
                    BEGIN
                        CREATE INDEX IX_UserTags_Tag ON dbo.UserTags(Tag) INCLUDE (UserId);
                    END;

                    IF OBJECT_ID(N'dbo.Products', N'U') IS NULL
                    BEGIN
                        CREATE TABLE dbo.Products
                        (
                            ProductId UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_Products PRIMARY KEY,
                            Name NVARCHAR(200) NOT NULL,
                            Description NVARCHAR(MAX) NULL,
                            Price DECIMAL(19, 4) NOT NULL,
                            StockQuantity INT NOT NULL
                        );
                    END;

                    IF OBJECT_ID(N'dbo.Orders', N'U') IS NULL
                    BEGIN
                        CREATE TABLE dbo.Orders
                        (
                            OrderId UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_Orders PRIMARY KEY,
                            CustomerName NVARCHAR(200) NOT NULL,
                            OrderDate DATETIME2 NOT NULL,
                            Status NVARCHAR(50) NOT NULL,
                            ProductId UNIQUEIDENTIFIER NOT NULL,
                            Quantity INT NOT NULL,
                            TotalAmount DECIMAL(19, 4) NOT NULL,
                            CONSTRAINT FK_Orders_Products FOREIGN KEY (ProductId)
                                REFERENCES dbo.Products(ProductId)
                        );
                    END;

                    IF NOT EXISTS
                    (
                        SELECT 1 FROM sys.foreign_keys
                        WHERE parent_object_id = OBJECT_ID(N'dbo.Orders')
                          AND name = N'FK_Orders_Products'
                    )
                    BEGIN
                        ALTER TABLE dbo.Orders WITH CHECK
                            ADD CONSTRAINT FK_Orders_Products FOREIGN KEY (ProductId)
                            REFERENCES dbo.Products(ProductId);
                    END;";
                connection.Open();
                command.ExecuteNonQuery();
            }
        }

        public void Save(User user)
        {
            using (var connection = CreateConnection(_providerFactory, _connectionString))
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction(IsolationLevel.Serializable))
                {
                    using (var command = connection.CreateCommand())
                    {
                        command.Transaction = transaction;
                        command.CommandText = @"
                            IF EXISTS (SELECT 1 FROM dbo.Users WITH (UPDLOCK, HOLDLOCK) WHERE Id = @id)
                                UPDATE dbo.Users
                                SET Name = @name, Email = @email, Type = @type,
                                    MonthlySalary = @monthlySalary, Age = @age
                                WHERE Id = @id;
                            ELSE
                                INSERT INTO dbo.Users (Id, Name, Email, Type, MonthlySalary, Age)
                                VALUES (@id, @name, @email, @type, @monthlySalary, @age);";
                        AddUserParameters(command, user);
                        command.ExecuteNonQuery();
                    }

                    using (var command = connection.CreateCommand())
                    {
                        command.Transaction = transaction;
                        command.CommandText = "DELETE FROM dbo.UserTags WHERE UserId = @id;";
                        AddParameter(command, "@id", DbType.Guid, user.Id);
                        command.ExecuteNonQuery();
                    }

                    using (var command = connection.CreateCommand())
                    {
                        command.Transaction = transaction;
                        command.CommandText = "INSERT INTO dbo.UserTags (UserId, Tag) VALUES (@id, @tag);";
                        AddParameter(command, "@id", DbType.Guid, user.Id);
                        var tagParameter = AddParameter(command, "@tag", DbType.String, DBNull.Value, -1);
                        foreach (var tag in user.Tags)
                        {
                            tagParameter.Value = (object)tag ?? DBNull.Value;
                            command.ExecuteNonQuery();
                        }
                    }

                    transaction.Commit();
                }
            }
        }

        public void Delete(User user)
        {
            using (var connection = CreateConnection(_providerFactory, _connectionString))
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "DELETE FROM dbo.Users WHERE Id = @id;";
                AddParameter(command, "@id", DbType.Guid, user.Id);
                connection.Open();
                command.ExecuteNonQuery();
            }
        }

        public User Get(Guid id)
        {
            return QueryUsers("u.Id = @id", command =>
                AddParameter(command, "@id", DbType.Guid, id)).SingleOrDefault();
        }

        public IEnumerable<User> Get(UserTypes? userType = null, string name = null, string email = null)
        {
            var filters = new List<string>();
            return QueryUsers(BuildUserFilters(filters, userType, name, email), command =>
            {
                if (userType.HasValue)
                {
                    AddParameter(command, "@type", DbType.Int32, (int)userType.Value);
                }

                if (name != null)
                {
                    AddParameter(command, "@name", DbType.String, "%" + name + "%", -1);
                }

                if (email != null)
                {
                    AddParameter(command, "@email", DbType.String, email, -1);
                }
            });
        }

        public IEnumerable<User> GetByTag(string tag)
        {
            return QueryUsers("EXISTS (SELECT 1 FROM dbo.UserTags t WHERE t.UserId = u.Id AND t.Tag = @tag)", command =>
                AddParameter(command, "@tag", DbType.String, (object)tag ?? DBNull.Value, -1));
        }

        public void DeleteAll()
        {
            using (var connection = CreateConnection(_providerFactory, _connectionString))
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "DELETE FROM dbo.Users;";
                connection.Open();
                command.ExecuteNonQuery();
            }
        }

        private IEnumerable<User> QueryUsers(string filter, Action<DbCommand> addParameters)
        {
            var query = @"
                SELECT u.Id, u.Name, u.Email, u.Type, u.MonthlySalary, u.Age, t.Tag
                FROM dbo.Users u
                LEFT JOIN dbo.UserTags t ON t.UserId = u.Id";
            if (!string.IsNullOrEmpty(filter))
            {
                query += " WHERE " + filter;
            }
            query += " ORDER BY u.Id;";

            using (var connection = CreateConnection(_providerFactory, _connectionString))
            using (var command = connection.CreateCommand())
            {
                command.CommandText = query;
                addParameters(command);
                connection.Open();
                using (var reader = command.ExecuteReader())
                {
                    return ReadUsers(reader);
                }
            }
        }

        private static IEnumerable<User> ReadUsers(DbDataReader reader)
        {
            var users = new Dictionary<Guid, User>();
            var tagsByUser = new Dictionary<Guid, List<string>>();
            while (reader.Read())
            {
                var id = reader.GetGuid(0);
                if (!users.TryGetValue(id, out var user))
                {
                    user = new User(id);
                    user.SetName(reader.GetString(1));
                    user.SetEmail(reader.GetString(2));
                    user.SetType((UserTypes)reader.GetInt32(3));
                    user.SetMonthlySalary(reader.IsDBNull(4) ? (decimal?)null : reader.GetDecimal(4));
                    user.SetAge(reader.GetInt32(5));
                    users.Add(id, user);
                    tagsByUser.Add(id, new List<string>());
                }

                if (!reader.IsDBNull(6))
                {
                    tagsByUser[id].Add(reader.GetString(6));
                }
            }

            foreach (var entry in users)
            {
                entry.Value.SetTags(tagsByUser[entry.Key]);
            }

            return users.Values.ToList();
        }

        private static void AddUserParameters(DbCommand command, User user)
        {
            AddParameter(command, "@id", DbType.Guid, user.Id);
            AddParameter(command, "@name", DbType.String, user.Name, -1);
            AddParameter(command, "@email", DbType.String, user.Email, -1);
            AddParameter(command, "@type", DbType.Int32, (int)user.Type);
            var salaryParameter = AddParameter(command, "@monthlySalary", DbType.Decimal, (object)user.MonthlySalary ?? DBNull.Value);
            AddParameter(command, "@age", DbType.Int32, user.Age);
        }

        private static DbParameter AddParameter(
            DbCommand command,
            string name,
            DbType type,
            object value,
            int? size = null)
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = name;
            parameter.DbType = type;
            if (size.HasValue)
            {
                parameter.Size = size.Value;
            }
            parameter.Value = value;
            command.Parameters.Add(parameter);
            return parameter;
        }

        private static DbConnection CreateConnection(DbProviderFactory providerFactory, string connectionString)
        {
            var connection = providerFactory.CreateConnection();
            if (connection == null)
            {
                throw new InvalidOperationException("The configured SQL Server provider could not create a connection.");
            }

            connection.ConnectionString = connectionString;
            return connection;
        }

        private static string BuildUserFilters(
            IList<string> filters,
            UserTypes? userType,
            string name,
            string email)
        {
            if (userType.HasValue)
            {
                filters.Add("u.Type = @type");
            }

            if (name != null)
            {
                filters.Add("u.Name LIKE @name");
            }

            if (email != null)
            {
                filters.Add("u.Email = @email");
            }

            return string.Join(" AND ", filters);
        }
    }
}
