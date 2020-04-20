using ANNProductSync.Models;
using ANNProductSync.Services;
using ANNProductSync.Services.FactoryPattern;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using WooCommerceNET;
using WooCommerceNET.WooCommerce.v3;

namespace ANNProductSync.Controllers
{
    [ApiController]
    [Route("api/v1/product")]
    public class ProductController : ControllerBase
    {
        #region Parameters
        private readonly ILogger<ProductController> _logger;
        private readonly ProductService _service;
        #endregion

        public ProductController(ILogger<ProductController> logger)
        {
            _logger = logger;
            _service = ANNFactoryService.getInstance<ProductService>();
        }

        #region Private
        private CheckHeaderRequestModel _checkHeaderRequest(IHeaderDictionary headers)
        {
            var result = new CheckHeaderRequestModel();

            if (!headers.ContainsKey("domain"))
            {
                result.statusCode = StatusCodes.Status400BadRequest;
                result.status = false;
                result.message = "Thiếu domain WooCommerce";

                return result;
            }

            var configuration = new ConfigurationBuilder()
                 .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                 .AddJsonFile("appsettings.json")
                 .Build();

            var domain = headers.Where(x => x.Key == "domain").Select(x => x.Value).FirstOrDefault();
            var domainSetting = configuration.GetSection(domain).Get<DomainSettingModel>();

            if (String.IsNullOrEmpty(domain))
            {
                result.statusCode = StatusCodes.Status400BadRequest;
                result.status = false;
                result.message = "Domain WooCommerce không được rỗng";

                return result;
            }
            if (domainSetting == null)
            {
                result.statusCode = StatusCodes.Status500InternalServerError;
                result.status = false;
                result.message = String.Format("{0} chưa được cài đặt", domain);

                return result;
            }

            var restAPI = new RestAPI(String.Format("https://{0}/wp-json/wc/v3/", domain), domainSetting.user, domainSetting.pass);
            var wcObject = new WCObject(restAPI);

            result.status = true;
            result.message = String.Empty;
            result.wc = new Models.WooCommerce()
            {
                restAPI = restAPI,
                wcObject = wcObject
            };
            result.priceType = domainSetting.priceType;

            return result;
        }

        private async Task<ProductTag> _createProductTag(WCObject wcObject, string tagName)
        {
            return await wcObject.Tag.Add(new ProductTag() { name = tagName });
        }

        /// <summary>
        /// Thực hiện post biến thể
        /// </summary>
        /// <param name="productVariation"></param>
        /// <param name="wcProduct"></param>
        /// <param name="wcObject"></param>
        /// <param name="priceType"></param>
        /// <returns></returns>
        private async Task<Variation> _postVariation(ProductVariationModel productVariation, Product wcProduct, WCObject wcObject, string priceType)
        {
            var image = new VariationImage();
            if (!String.IsNullOrEmpty(productVariation.image))
            {
                var wcImageID = wcProduct.images.Where(x => x.name == productVariation.image).Select(x => x.id).FirstOrDefault();

                if (wcImageID.HasValue)
                    image.id = Convert.ToInt32(wcImageID);
                else
                    image.src = String.Format("http://hethongann.com/uploads/images/{0}", productVariation.image);

                image.alt = wcProduct.name;
            }
            else
            {
                image = null;
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
            Variation wcProductVariation = await wcObject.Product.Variations.Add(new Variation()
            {
                sku = productVariation.sku,
                regular_price = priceType == PriceType.WholesalePrice ? productVariation.regular_price : productVariation.retail_price,
                manage_stock = true,
                stock_quantity = productVariation.stock_quantity,
                image = image,
                attributes = attributes
            },
            wcProduct.id.Value);

            return wcProductVariation;
        }
        #endregion

        #region Public
        #region Post
        /// <summary>
        /// Thực hiện post product
        /// </summary>
        /// <param name="productID"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("{productID}")]
        public async Task<IActionResult> postProduct(int productID)
        {
            #region Kiểm tra điều kiện header request
            var checkHeader = _checkHeaderRequest(Request.Headers);

            if (!checkHeader.status)
                return StatusCode(checkHeader.statusCode, checkHeader.message);

            var wcObject = checkHeader.wc.wcObject;
            #endregion

            #region Kiểm tra tồn tại sản phẩm trong data gốc
            var product = _service.getProductByID(productID);

            if (product == null)
                return BadRequest("Không tìm thấy sản phẩm");
            #endregion

            #region Thực hiện post sản phẩm product
            #region Regular Price
            decimal? regular_price = null;

            if (product.type == ProductType.Simple)
            {
                if (checkHeader.priceType == PriceType.WholesalePrice)
                    regular_price = Convert.ToDecimal(product.regularPrice);
                else
                    regular_price = Convert.ToDecimal(product.retailPrice);
            }    

            #endregion
            
            #region Category List
            var categories = new List<ProductCategoryLine>();
            var wcProductCategory = await wcObject.Category.GetAll(new Dictionary<string, string>() {{ "search", product.categoryName} });
            if (wcProductCategory.Count > 0)
            {
                var wcProductCategoryID = wcProductCategory.Select(x => x.id).FirstOrDefault();
                categories.Add(new ProductCategoryLine() { id = wcProductCategoryID });
            }
            #endregion

            #region Tag List
            var tags = new List<ProductTagLine>();
            foreach (var tag in product.tags)
            {
                var wcTags = await wcObject.Tag.GetAll(new Dictionary<string, string>()
                {
                    {"per_page", "100"},
                    {"search", tag.name}
                });

                if (wcTags != null && wcTags.Count > 0)
                {
                    var wcTag = wcTags.Where(x => x.name == tag.name).FirstOrDefault();

                    if (wcTag != null)
                    {
                        tags.Add(new ProductTagLine() { id = wcTag.id });
                        continue;
                    }

                }

                var wcTagNew = await _createProductTag(wcObject, tag.name);

                if (wcTagNew != null)
                    tags.Add(new ProductTagLine() { id = wcTagNew.id });
            }
            #endregion

            #region Image List
            var images = new List<ProductImage>();
            if (!String.IsNullOrEmpty(product.avatar))
                images.Add(new ProductImage() { src = String.Format("http://hethongann.com/uploads/images/{0}", product.avatar), alt = product.name, position = 0 });

            if (product.images.Count > 0)
            {
                product.images = product.images.Distinct().ToList();

                if (!String.IsNullOrEmpty(product.avatar))
                {
                    var avatar = product.images.Where(x => x == product.avatar).FirstOrDefault();
                    if (!String.IsNullOrEmpty(avatar))
                        product.images.Remove(avatar);
                }

                if (product.images.Count > 0)
                    images.AddRange(product.images.Select(x => new ProductImage() { src = String.Format("http://hethongann.com/uploads/images/{0}", x), alt = product.name }).ToList());
            }
            #endregion

            #region Attribute List
            var attributes = new List<ProductAttributeLine>();
            if (product.type == ProductType.Variable)
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
            #endregion

            //Add new product
            Product wcProduct = await wcObject.Product.Add(new Product()
            {
                name = product.name,
                sku = product.sku,
                regular_price = regular_price,
                type = product.type,
                description = product.content,
                short_description = product.materials,
                categories = categories,
                tags = tags,
                images = images,
                attributes = attributes,
                manage_stock = product.manage_stock,
                stock_quantity = product.stock_quantity
            });
            #endregion

            #region Thực hiện post các biến thể nếu là sản phẩm biến thể
            if (product.type == ProductType.Variable && wcProduct != null)
            {
                var productVariationList = _service.getProductVariationByProductID(productID);

                foreach (var productVariation in productVariationList)
                {
                    await _postVariation(productVariation, wcProduct, wcObject, checkHeader.priceType);
                }
            }
            #endregion

            return Ok(wcProduct);
        }

        /// <summary>
        /// Thực hiện post biến thể của product
        /// </summary>
        /// <param name="productID"></param>
        /// <param name="variationSKU"></param>
        /// <param name="wcProductID"></param>
        /// <returns></returns>
        [HttpPost]
        [Produces("application/json")]
        [Route("{productID}/variation/{variationSKU}")]
        public async Task<IActionResult> postProductVariation(int productID, string variationSKU, [FromBody]PostProductVariationParameter parameters)
        {
            #region Kiểm tra điều kiện header request
            var checkHeader = _checkHeaderRequest(Request.Headers);

            if (!checkHeader.status)
                return StatusCode(checkHeader.statusCode, checkHeader.message);

            var wcObject = checkHeader.wc.wcObject;
            #endregion

            #region Kiểm tra xem biến thể có tồn database gốc không
            var productVariation = _service.getProductVariationByProductID(productID, variationSKU);

            if (productVariation == null)
                return BadRequest("Không tìm thấy biến thể");
            #endregion

            #region Kiểm tra xem có biến thể cha ở WooCommerce không
            //Get products
            var wcProduct = await wcObject.Product.Get(parameters.wcProductID);

            if (wcProduct == null)
                return BadRequest("Không tìm thấy sản cha " + parameters.wcProductID);
            #endregion
           
            #region Thức thi post sản phẩm biến thể
            //Add new product variation
            Variation wcProductVariation = await _postVariation(productVariation, wcProduct, wcObject, checkHeader.priceType);
            #endregion

            return Ok(wcProductVariation);
        }
        #endregion
        #region Get
        /// <summary>
        /// Thực hiện post product
        /// </summary>
        /// <param name="slug"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("{productID}")]
        public async Task<IActionResult> getProduct(int productID)
        {
            #region Kiểm tra điều kiện header request
            var checkHeader = _checkHeaderRequest(Request.Headers);

            if (!checkHeader.status)
                return StatusCode(checkHeader.statusCode, checkHeader.message);

            var wcObject = checkHeader.wc.wcObject;
            #endregion

            #region Kiểm tra tồn tại sản phẩm trong data gốc
            var product = _service.getProductByID(productID);

            if (product == null)
                return BadRequest("Không tìm thấy sản phẩm hệ thống gốc");
            #endregion

            #region Thực hiện get sản phẩm
            var wcProduct = await wcObject.Product.GetAll(new Dictionary<string, string>() { { "sku", product.sku } });
            #endregion

            return Ok(wcProduct);
        }
        #endregion
        #endregion
    }
}
