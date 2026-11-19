using System;
using System.Linq;
using System.Net.Http;
using System.Web.Http;
using Core.Services.Products;
using WebApi.Models.Products;

namespace WebApi.Controllers
{
    [RoutePrefix("products")]
    public class ProductController : BaseApiController
    {
        private readonly IProductService _productService;

        public ProductController(IProductService productService)
        {
            _productService = productService;
        }

        [Route("{productId:guid}/create")]
        [HttpPost]
        public HttpResponseMessage Create(Guid productId, [FromBody] ProductModel model)
        {
            var product = _productService.Create(productId, model.Name, model.Description, model.Price, model.StockQuantity);
            return Found(new ProductData(product));
        }

        [Route("{productId:guid}/update")]
        [HttpPost]
        public HttpResponseMessage Update(Guid productId, [FromBody] ProductModel model)
        {
            var product = _productService.Update(productId, model.Name, model.Description, model.Price, model.StockQuantity);
            return product == null ? DoesNotExist("Product Does Not Exist") : Found(new ProductData(product));
        }

        [Route("{productId:guid}/delete")]
        [HttpDelete]
        public HttpResponseMessage Delete(Guid productId)
        {
            var product = _productService.Get(productId);
            if (product == null) return DoesNotExist("Product Does Not Exist");
            _productService.Delete(productId);
            return Found();
        }

        [Route("{productId:guid}")]
        [HttpGet]
        public HttpResponseMessage Get(Guid productId)
        {
            var product = _productService.Get(productId);
            return product == null ? DoesNotExist("Product Does Not Exist") : Found(new ProductData(product));
        }

        [Route("list")]
        [HttpGet]
        public HttpResponseMessage List(int skip = 0, int take = 100, string name = null, decimal? minPrice = null, decimal? maxPrice = null, bool? inStock = null)
        {
            var products = _productService.GetList(name, minPrice, maxPrice, inStock)
                                           .Skip(skip).Take(take)
                                           .Select(product => new ProductData(product))
                                           .ToList();
            return Found(products);
        }
    }
}
