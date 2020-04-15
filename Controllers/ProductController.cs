using ANNProductSync.Models;
using ANNProductSync.Services;
using ANNProductSync.Services.FactoryPattern;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WooCommerceNET;
using WooCommerceNET.WooCommerce.v3;
using WooCommerceNET.WooCommerce.v3.Extension;

namespace ANNProductSync.Controllers
{
    [ApiController]
    [Route("api/v1/product")]
    public class ProductController : ControllerBase
    {
        private readonly ILogger<ProductController> _logger;
        private readonly ProductService _service;

        private RestAPI _rest;
        private WCObject _wc;

        public ProductController(ILogger<ProductController> logger)
        {
            _logger = logger;
            _service = ANNFactoryService.getInstance<ProductService>();
        }


        [HttpPost]
        [Route("{slug}")]
        public async Task<IActionResult> postProduct(string slug)
        {
            if (!Request.Headers.ContainsKey("domain"))
                return BadRequest("Thiếu domain WooCommerce");
            if (!Request.Headers.ContainsKey("user"))
                return BadRequest("Thiếu user WooCommerce");
            if (!Request.Headers.ContainsKey("pass"))
                return BadRequest("Thiếu pass WooCommerce");
            
            var domain = Request.Headers.Where(x => x.Key == "domain").Select(x => x.Value).FirstOrDefault();
            var user = Request.Headers.Where(x => x.Key == "user").Select(x => x.Value).FirstOrDefault();
            var pass = Request.Headers.Where(x => x.Key == "pass").Select(x => x.Value).FirstOrDefault();

            if (String.IsNullOrEmpty(domain))
                return BadRequest("Domain WooCommerce không được rỗng");
            if (String.IsNullOrEmpty(user))
                return BadRequest("User WooCommerce không được rỗng");
            if (String.IsNullOrEmpty(pass))
                return BadRequest("Pass WooCommerce không được rỗng");

            _rest = new RestAPI(domain, user, pass);
            _wc = new WCObject(_rest);

            var product = _service.getProductBySlug(slug);

            if (product == null)
                return BadRequest("Không tìm thấy sản phẩm");

            // Category List
            var categories = new List<ProductCategoryLine>();
            categories.Add(new ProductCategoryLine() { id = 44 });

            // Image List
            var images = new List<ProductImage>();
            if (!String.IsNullOrEmpty(product.avatar))
                images.Add(new ProductImage() { src = String.Format("http://hethongann.com/uploads/images/{0}", product.avatar) });
            if (product.images.Count > 0)
                images.AddRange(product.images.Select(x => new ProductImage() { src = String.Format("http://hethongann.com/uploads/images/{0}", x) }).ToList());

            // Attribute List
            var attributes = new List<ProductAttributeLine>();
            if (product.type == "variable")
            {
                if (product.colors.Count > 0)
                {
                    attributes.Add(new ProductAttributeLine()
                    {
                        id = 1,
                        position = 0,
                        visible = true,
                        variation = true,
                        options = product.colors.Select(x => x.name).ToList(),
                    });
                }

                if (product.sizes.Count > 0)
                {
                    attributes.Add(new ProductAttributeLine()
                    {
                        id = 2,
                        position = 1,
                        visible = true,
                        variation = true,
                        options = product.sizes.Select(x => x.name).ToList(),
                    });
                }
            }

            //Add new product
            Product wcProduct = await _wc.Product.Add(new Product()
            {
                name = product.name,
                sku = product.sku,
                type = product.type,
                description = product.content,
                short_description = product.materials,
                categories = categories,
                images = images,
                attributes = attributes,
                manage_stock = product.manage_stock,
                stock_quantity = product.stock_quantity,
            });

            if (product.type == "variable" && wcProduct != null)
            {
                var productVariationList = _service.getProductVariationByProductSlug(slug);

                foreach (var productVariation in productVariationList)
                {
                    await _postVariation(productVariation, wcProduct);
                }
            }

            return Ok(wcProduct);
        }

        [HttpPost]
        [Route("{productSlug}/variation/{variationSKU}")]
        public async Task<IActionResult> postProductVariation(string productSlug, string variationSKU, [FromForm]int wcProductID)
        {
            if (!Request.Headers.ContainsKey("domain"))
                return BadRequest("Thiếu domain WooCommerce");
            if (!Request.Headers.ContainsKey("user"))
                return BadRequest("Thiếu user WooCommerce");
            if (!Request.Headers.ContainsKey("pass"))
                return BadRequest("Thiếu pass WooCommerce");

            var domain = Request.Headers.Where(x => x.Key == "domain").Select(x => x.Value).FirstOrDefault();
            var user = Request.Headers.Where(x => x.Key == "user").Select(x => x.Value).FirstOrDefault();
            var pass = Request.Headers.Where(x => x.Key == "pass").Select(x => x.Value).FirstOrDefault();

            if (String.IsNullOrEmpty(domain))
                return BadRequest("Domain WooCommerce không được rỗng");
            if (String.IsNullOrEmpty(user))
                return BadRequest("Userr WooCommerce không được rỗng");
            if (String.IsNullOrEmpty(pass))
                return BadRequest("Pass WooCommerce không được rỗng");

            _rest = new RestAPI(domain, user, pass);
            _wc = new WCObject(_rest);

            var productVariation = _service.getProductVariationByProductSlug(productSlug).Where(x => x.sku == variationSKU).FirstOrDefault();

            if (productVariation == null)
                return BadRequest("Không tìm thấy biến thể");

            //Get products
            var wcProduct = await _wc.Product.Get(wcProductID);

            if (wcProduct == null)
                return BadRequest("Không tìm thấy sản cha " + wcProductID);

            //Add new product variation
            Variation wcProductVariation = await _postVariation(productVariation, wcProduct);

            return Ok(wcProductVariation);
        }

        private async Task<Variation> _postVariation(ProductVariationModel productVariation, Product wcProduct)
        {
            var image = new VariationImage();
            if (!String.IsNullOrEmpty(productVariation.image))
            {
                var wcImageID = wcProduct.images.Where(x => x.name == productVariation.image).Select(x => x.id).FirstOrDefault();

                if (wcImageID.HasValue)
                    image.id = Convert.ToInt32(wcImageID);
                else
                    image.src = String.Format("http://hethongann.com/uploads/images/{0}", productVariation.image);
            }

            // Attribute List
            var attributes = new List<VariationAttribute>();
            if (!String.IsNullOrEmpty(productVariation.color))
            {
                attributes.Add(new VariationAttribute()
                {
                    id = 1,
                    option = productVariation.color,
                });
            }

            if (!String.IsNullOrEmpty(productVariation.size))
            {
                attributes.Add(new VariationAttribute()
                {
                    id = 2,
                    option = productVariation.size,
                });
            }

            //Add new product variation
            Variation wcProductVariation = await _wc.Product.Variations.Add(new Variation()
            {
                sku = productVariation.sku,
                regular_price = productVariation.regular_price,
                manage_stock = true,
                stock_quantity = productVariation.stock_quantity,
                image = image,
                attributes = attributes
            },
            wcProduct.id.Value);

            return wcProductVariation;
        }
    }
}
