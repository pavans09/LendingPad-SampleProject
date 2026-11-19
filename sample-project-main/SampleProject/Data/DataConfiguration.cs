using System.Data.Common;
using Data.Repositories;
using SimpleInjector;

namespace Data
{
    public class DataConfiguration
    {
        public static void Initialize(
            Container container,
            Lifestyle lifestyle,
            string userDatabaseConnectionString,
            DbProviderFactory providerFactory)
        {
            UserRepository.InitializeDatabase(userDatabaseConnectionString, providerFactory);
            container.Register<IUserRepository>(
                () => new UserRepository(userDatabaseConnectionString, providerFactory),
                lifestyle);
            container.Register<IProductRepository>(
                () => new ProductRepository(userDatabaseConnectionString, providerFactory),
                lifestyle);
            container.Register<IOrderRepository>(
                () => new OrderRepository(userDatabaseConnectionString, providerFactory),
                lifestyle);
        }
    }
}