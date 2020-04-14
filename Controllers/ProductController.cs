using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ANNProductSync.Services;
using ANNProductSync.Services.FactoryPattern;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

using ANNProductSync.Models;

namespace ANNProductSync.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ProductController : ControllerBase
    {
        private readonly ILogger<ProductController> _logger;
        private readonly ProductService _service;

        public ProductController(ILogger<ProductController> logger)
        {
            _logger = logger;
            _service = ANNFactoryService.getInstance<ProductService>();
        }

        [HttpGet]
        public IActionResult Get()
        {
            var filter = new ProductFilterModel();
            var pagination = new PaginationMetadataModel() { 
                currentPage = 1,
                pageSize = 10
            };
            var result = _service.getProducts(filter, ref pagination);

            return Ok(result);
        }
    }
}
