using BusinessEntities;
using Core.Services.Orders;
using System;
using System.Linq;
using System.Net.Http;
using System.Web.Http;
using WebApi.Models.Orders;
using WebApi.Models.Products;

namespace WebApi.Controllers
{
    [RoutePrefix("orders")]
    public class OrderController : BaseApiController
    {
        private readonly IOrderService _orderService;

        public OrderController(IOrderService orderService)
        {
            _orderService = orderService;
        }

        [Route("{orderId:guid}/create")]
        [HttpPost]
        public HttpResponseMessage Create(Guid orderId, [FromBody] OrderModel model)
        {
            var order = _orderService.Create(orderId, model.CustomerName, model.OrderDate, model.Status, model.ProductId, model.Quantity);
            return Found(new OrderData(order));
        }

        [Route("{orderId:guid}/update")]
        [HttpPost]
        public HttpResponseMessage Update(Guid orderId, [FromBody] OrderModel model)
        {
            var order = _orderService.Update(orderId, model.CustomerName, model.OrderDate, model.Status, model.ProductId, model.Quantity);
            return order == null ? DoesNotExist("Order Does Not Exist") : Found(new OrderData(order));
        }

        [Route("{orderId:guid}/delete")]
        [HttpDelete]
        public HttpResponseMessage Delete(Guid orderId)
        {
            var order = _orderService.Get(orderId);
            if (order == null) return DoesNotExist("Order Does Not Exist");
            _orderService.Delete(orderId);
            return Found();
        }

        [Route("{orderId:guid}")]
        [HttpGet]
        public HttpResponseMessage Get(Guid orderId)
        {
            var order = _orderService.Get(orderId);
            return order == null ? DoesNotExist("order Does Not Exist") : Found(new OrderData(order));
        }

        [Route("list")]
        [HttpGet]
        public HttpResponseMessage List(int skip = 0, int take = 100, string customerName = null, string status = null, DateTime? fromDate = null, DateTime? toDate = null, Guid? productId = null)
        {
            var orders = _orderService.GetList(customerName, status, fromDate, toDate, productId)
                                       .Skip(skip).Take(take)
                                       .Select(order => new OrderData(order))
                                       .ToList();
            return Found(orders);
        }
    }
}
