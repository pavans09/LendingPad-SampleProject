using System;
using System.Collections.Generic;
using BusinessEntities;

namespace Data.Repositories
{
    public interface IOrderRepository
    {
        void Save(Order order);
        void Delete(Guid id);
        Order Get(Guid id);
        IEnumerable<Order> Get(string customerName = null, string status = null, DateTime? fromDate = null, DateTime? toDate = null, Guid? productId = null);
    }
}
