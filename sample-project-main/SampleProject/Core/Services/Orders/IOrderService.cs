using System;
using System.Collections.Generic;
using BusinessEntities;

namespace Core.Services.Orders
{
    public interface IOrderService
    {
        Order Create(Guid id, string customerName, DateTime? orderDate, string status, Guid productId, int quantity);
        Order Update(Guid id, string customerName, DateTime? orderDate, string status, Guid productId, int quantity);
        void Delete(Guid id);
        Order Get(Guid id);
        IEnumerable<Order> GetList(string customerName = null, string status = null, DateTime? fromDate = null, DateTime? toDate = null, Guid? productId = null);
    }
}
