using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;
using AspNetCore.WebSocket.RESTfulAPI.Models;

namespace AspNetCore.WebSocket.RESTfulAPI.TestServer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [ApiExplorerSettings(GroupName = "APIs")]
    public class ProductController : ControllerBase
    {
        private static readonly string[] Products = new[]
        {
            "Laptop", "Keyboard", "Apple", "T-Shirt"
        };

        private readonly ILogger<ProductController> _logger;

        public ProductController(ILogger<ProductController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public async Task<ResponseModel> Get()
        {
            var rng = new Random();
            return await ResponseModel.SuccessRequestAsync(Products.Select(item => new Product
            {
                Name = item,
                Price = rng.Next(50, 500)
            })
            .ToArray());
        }
    }
}
