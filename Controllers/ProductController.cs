using ANNwpsync.Models;
using ANNwpsync.Services;
using ANNwpsync.Services.FactoryPattern;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using WooCommerceNET;
using WooCommerceNET.WooCommerce.v3;

namespace ANNwpsync.Controllers
{
    [ApiController]
    [Route("api/v1/woocommerce")]
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
                result.success = false;
                result.message = "Thiếu domain";

                return result;
            }

            var configuration = new ConfigurationBuilder()
                 .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                 .AddJsonFile("appsettings.json")
                 .Build();

            var domain = headers.Where(x => x.Key == "domain").Select(x => x.Value).FirstOrDefault();
            var domainSetting = configuration.GetSection(domain).Get<DomainSettingModel>();

            var ANNconfig = configuration.GetSection("ANNconfig").Get<ANNConfigModel>();

            if (String.IsNullOrEmpty(domain))
            {
                result.statusCode = StatusCodes.Status400BadRequest;
                result.success = false;
                result.message = "Domain không được rỗng";

                return result;
            }
            if (domainSetting == null)
            {
                result.statusCode = StatusCodes.Status500InternalServerError;
                result.success = false;
                result.message = String.Format("{0} chưa được cài đặt", domain);

                return result;
            }

            var restAPI = new RestAPI(String.Format("https://{0}/wp-json/wc/v3/", domain), domainSetting.woocommerce_key, domainSetting.woocommerce_secret, false);
            var wcObject = new WCObject(restAPI);

            result.domain = domain;
            result.success = true;
            result.message = String.Empty;
            result.mainDomain = ANNconfig.main_domain;
            result.wc = new Models.WooCommerce()
            {
                restAPI = restAPI,
                wcObject = wcObject
            };
            result.priceType = domainSetting.woocommerce_price_type;
            return result;
        }

        private async Task<ProductTag> _createWCProductTag(WCObject wcObject, string tagName)
        {
            return await wcObject.Tag.Add(new ProductTag() { name = tagName });
        }

        private async Task<ProductAttribute> _getWCAttribute(WCObject wcObject, string name)
        {
            var attr = await wcObject.Attribute.GetAll();

            if (attr == null)
                return null;

            return attr.Where(x => x.name == name).FirstOrDefault();

        }
        /// <summary>
        /// Thực hiện post biến thể
        /// </summary>
        /// <param name="productVariation"></param>
        /// <param name="wcProduct"></param>
        /// <param name="wcObject"></param>
        /// <param name="priceType"></param>
        /// <returns></returns>
        private async Task<Variation> _postWCVariation(ProductVariationModel productVariation, Product wcProduct, WCObject wcObject, string priceType)
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
                var color = await _getWCAttribute(wcObject, "Màu");
                int colorID = color != null ? color.id.Value : 1;
                attributes.Add(new VariationAttribute()
                {
                    id = colorID,
                    option = productVariation.color,
                });
            }

            if (!String.IsNullOrEmpty(productVariation.size))
            {
                var size = await _getWCAttribute(wcObject, "Size");
                int sizeID = size != null ? size.id.Value : 2;
                attributes.Add(new VariationAttribute()
                {
                    id = sizeID,
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
        private string _renameProduct(string domain, int productID, string productName)
        {
            string ID = productID.ToString().Substring(productID.ToString().Length - 1);
            string addBefore = "";

            switch (domain)
            {
                case "quanaogiaxuong.com":
                    switch (ID)
                    {
                        case "1":
                            addBefore = "Nguồn sỉ";
                            break;
                        case "3":
                            addBefore = "Buôn sỉ";
                            break;
                        case "5":
                            addBefore = "Xưởng chuyên sỉ";
                            break;
                        case "7":
                            addBefore = "Kho sỉ";
                            break;
                        case "9":
                            addBefore = "Nhập sỉ";
                            break;
                        default:
                            break;
                    }
                    break;
                case "annshop.vn":
                    switch (ID)
                    {
                        case "2":
                            addBefore = "Chuyên sỉ";
                            break;
                        case "4":
                            addBefore = "Nguồn hàng sỉ";
                            break;
                        case "6":
                            addBefore = "Nơi lấy sỉ";
                            break;
                        case "8":
                            addBefore = "Chỗ sỉ";
                            break;
                        case "0":
                            addBefore = "Shop sỉ";
                            break;
                        default:
                            break;
                    }
                    break;
                case "quanaoxuongmay.com":
                    switch (ID)
                    {
                        case "1":
                            addBefore = "Xưởng sỉ";
                            break;
                        case "3":
                            addBefore = "Top 1 kho sỉ";
                            break;
                        case "5":
                            addBefore = "Nguồn chuyên sỉ";
                            break;
                        case "7":
                            addBefore = "Cần lấy sỉ";
                            break;
                        case "9":
                            addBefore = "Shop chuyên sỉ";
                            break;
                        default:
                            break;
                    }
                    break;
                case "panpan.vn":
                    switch (ID)
                    {
                        case "0":
                            addBefore = "Tìm sỉ";
                            break;
                        case "3":
                            addBefore = "Kho sỉ";
                            break;
                        case "6":
                            addBefore = "Xưởng bỏ sỉ";
                            break;
                        case "9":
                            addBefore = "Giá sỉ";
                            break;
                        case "8":
                            addBefore = "Muốn lấy sỉ";
                            break;
                        default:
                            break;
                    }
                    break;
                case "bansithoitrang.net":
                    switch (ID)
                    {
                        case "0":
                            addBefore = "Chuyên sỉ";
                            break;
                        case "2":
                            addBefore = "Giá sỉ";
                            break;
                        case "4":
                            addBefore = "Nguồn hàng sỉ";
                            break;
                        case "6":
                            addBefore = "Kho sỉ";
                            break;
                        case "8":
                            addBefore = "Tìm sỉ";
                            break;
                        default:
                            break;
                    }
                    break;
                case "ann.com.vn":
                    switch (ID)
                    {
                        case "1":
                            addBefore = "Xưởng sỉ";
                            break;
                        case "3":
                            addBefore = "Shop sỉ";
                            break;
                        case "5":
                            addBefore = "Kho sỉ";
                            break;
                        case "7":
                            addBefore = "Giá sỉ";
                            break;
                        case "9":
                            addBefore = "Chuyên sỉ";
                            break;
                        default:
                            break;
                    }
                    break;
                case "khohangsiann.com":
                    switch (ID)
                    {
                        case "0":
                            addBefore = "Nguồn sỉ";
                            break;
                        case "2":
                            addBefore = "Xưởng sỉ";
                            break;
                        case "4":
                            addBefore = "Chợ sỉ";
                            break;
                        case "6":
                            addBefore = "Giá sỉ";
                            break;
                        case "8":
                            addBefore = "Shop sỉ";
                            break;
                        default:
                            break;
                    }
                    break;
                case "bosiquanao.net":
                    switch (ID)
                    {
                        case "0":
                            addBefore = "Bỏ sỉ";
                            break;
                        case "3":
                            addBefore = "Phân phối";
                            break;
                        case "6":
                            addBefore = "Nguồn sỉ";
                            break;
                        case "9":
                            addBefore = "Chuyên sỉ";
                            break;
                        case "5":
                            addBefore = "Giá sỉ";
                            break;
                        default:
                            break;
                    }
                    break;
                case "thoitrangann.com":
                    switch (ID)
                    {
                        case "0":
                            addBefore = "Kho sỉ";
                            break;
                        case "2":
                            addBefore = "Xưởng sỉ";
                            break;
                        case "3":
                            addBefore = "Giá sỉ";
                            break;
                        case "7":
                            addBefore = "Bán Sỉ";
                            break;
                        case "8":
                            addBefore = "Chợ sỉ";
                            break;
                        default:
                            break;
                    }
                    break;
                case "nhapsionline.com":
                    switch (ID)
                    {
                        case "0":
                            addBefore = "Bán sỉ";
                            break;
                        case "1":
                            addBefore = "Nhập sỉ";
                            break;
                        case "4":
                            addBefore = "Xưởng sỉ";
                            break;
                        case "5":
                            addBefore = "Nguồn hàng";
                            break;
                        case "7":
                            addBefore = "Shop sỉ";
                            break;
                        default:
                            break;
                    }
                    break;
                case "simyphamonline.com":
                    switch (ID)
                    {
                        case "0":
                            addBefore = "Review";
                            break;
                        case "1":
                            addBefore = "Giá sỉ";
                            break;
                        case "4":
                            addBefore = "Phân phối";
                            break;
                        case "7":
                            addBefore = "Tổng phân phối";
                            break;
                        default:
                            break;
                    }
                    break;
                default:
                    break;
            }

            if (!string.IsNullOrEmpty(productName) && !string.IsNullOrEmpty(addBefore))
            {
                productName = addBefore + " " + char.ToLower(productName[0]) + productName.Substring(1);
            }
            
            return productName;
        }
        /// <summary>
        /// Thực hiện xử lý sản phẩm
        /// </summary>
        /// <param name="product"></param>
        /// <param name="wcObject"></param>
        /// <param name="priceType"></param>
        /// <param name="domain"></param>
        /// <returns></returns>
        private async Task<Product> _handleWCProduct(ProductModel product, WCObject wcObject, string priceType, string domain)
        {
            #region Regular Price
            decimal? regular_price = null;

            if (product.type == ProductType.Simple)
            {
                if (priceType == PriceType.WholesalePrice)
                    regular_price = Convert.ToDecimal(product.regularPrice);
                else
                    regular_price = Convert.ToDecimal(product.retailPrice);
            }

            #endregion

            #region Category List
            var categories = new List<ProductCategoryLine>();
            var wcProductCategory = await wcObject.Category.GetAll(new Dictionary<string, string>() { { "search", product.categoryName } });
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
                if (tag.name == "hot")
                {
                    continue;
                }

                var wcTags = await wcObject.Tag.GetAll(new Dictionary<string, string>()
                {
                    {"per_page", "50"},
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

                var wcTagNew = await _createWCProductTag(wcObject, tag.name);

                if (wcTagNew != null)
                    tags.Add(new ProductTagLine() { id = wcTagNew.id });
            }
            #endregion

            #region Image List
            var images = new List<ProductImage>();
            var imageNameList = new List<ProductImage>();
            if (!String.IsNullOrEmpty(product.avatar))
            {
                images.Add(new ProductImage() { src = String.Format("http://hethongann.com/uploads/images/{0}", product.avatar), alt = product.name, position = 0 });
                imageNameList.Add(new ProductImage() { src = product.avatar, alt = product.name, position = 0 });
            }

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
                {
                    images.AddRange(product.images.Select(x => new ProductImage() { src = String.Format("http://hethongann.com/uploads/images/{0}", x), alt = product.name }).ToList());
                    imageNameList.AddRange(product.images.Select(x => new ProductImage() { src = x, alt = product.name }).ToList());
                }

            }
            #endregion

            #region Attribute List
            var attributes = new List<ProductAttributeLine>();
            if (product.type == ProductType.Variable)
            {
                if (product.colors.Count > 0)
                {
                    var color = await _getWCAttribute(wcObject, "Màu");
                    int colorID = color != null ? color.id.Value : 1;

                    attributes.Add(new ProductAttributeLine()
                    {
                        id = colorID,
                        position = 0,
                        visible = true,
                        variation = true,
                        options = product.colors.Select(x => x.name).ToList(),
                    });
                }

                if (product.sizes.Count > 0)
                {
                    var size = await _getWCAttribute(wcObject, "Size");
                    int sizeID = size != null ? size.id.Value : 1;

                    attributes.Add(new ProductAttributeLine()
                    {
                        id = sizeID,
                        position = 1,
                        visible = true,
                        variation = true,
                        options = product.sizes.Select(x => x.name).ToList(),
                    });
                }
            }
            #endregion

            #region Product Name
            string productName = _renameProduct(domain, product.id, product.name);
            #endregion

            #region Content
            string productContent = "";
            string shortDescription = product.short_description + "<br>";

            // Video
            var videoList = _service.getVideoByProductID(product.id);
            if (videoList.Count > 0)
            {
                foreach (var item in videoList)
                {
                    productContent += String.Format("<iframe width='560' height='315' src='https://www.youtube.com/embed/{0}?controls=0' title='{1}' frameborder='0' allow='accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture' allowfullscreen></iframe><br>", item.VideoId, product.name);
                }
            }

            // Materials
            string[] noMaterials = { "my-pham", "kem-face", "kem-body", "serum", "tam-trang", "sua-tam", "sua-rua-mat", "dau-goi-dau", "son-moi", "kem-chong-nang", "my-pham-tong-hop", "nuoc-hoa", "nuoc-hoa-charme", "nuoc-hoa-noi-dia-trung", "nuoc-hoa-vung-kin", "nuoc-hoa-gia-re", "bao-li-xi-tet", "thuc-pham-chuc-nang", "tong-hop" };
            if (!noMaterials.Contains(product.categorySlug))
            {
                productContent += "Chất liệu " + product.materials + ".<br><br>";
                shortDescription += "Chất liệu " + product.materials;
            }

            productContent += product.content + "<br>";
            #endregion

            return new Product()
            {
                name = productName,
                sku = product.sku,
                regular_price = regular_price,
                type = product.type,
                description = productContent,
                short_description = shortDescription,
                categories = categories,
                tags = tags,
                images = images,
                attributes = attributes,
                manage_stock = product.manage_stock,
                stock_quantity = product.stock_quantity,
                meta_data = new List<WooCommerceNET.WooCommerce.v2.ProductMeta>()
                            {
                                new WooCommerceNET.WooCommerce.v2.ProductMeta()
                                {
                                    key = "_retail_price",
                                    value = product.retailPrice
                                }
                            }
            };
            }
        #endregion

        #region Public
        #region Post Product
        /// <summary>
        /// Thực hiện post sản phẩm
        /// </summary>
        /// <param name="productID"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("product/{productID:int}")]
        public async Task<IActionResult> postProduct(int productID)
        {
            #region Kiểm tra điều kiện header request
            var checkHeader = _checkHeaderRequest(Request.Headers);

            if (!checkHeader.success)
                return StatusCode(checkHeader.statusCode, checkHeader.message);

            var wcObject = checkHeader.wc.wcObject;
            #endregion

            #region Kiểm tra tồn tại sản phẩm trong data gốc
            var product = _service.getProductByID(productID);

            if (product == null)
                return BadRequest(new ResponseModel() { success = false, message = "Không tìm thấy sản phẩm" });
            #endregion

            #region Thực hiện post sản phẩm
            try
            {
                //Add new product
                Product newProduct = await _handleWCProduct(product, wcObject, checkHeader.priceType, checkHeader.domain);
                Product wcProduct = await wcObject.Product.Add(newProduct);

                #region Update hình trong nội dung sản phẩm
                string productContent = wcProduct.description + "<h3>" + product.name + "</h3>";
                wcProduct.images.RemoveAt(0);
                foreach (var item in wcProduct.images)
                {
                    productContent += String.Format("<p><img src='/wp-content/uploads/{0}' alt='{1}' /></p>", System.IO.Path.GetFileName(item.src), product.name);
                }
                var updateProduct = await wcObject.Product.Update(wcProduct.id.Value, new Product { description = productContent });
                #endregion

                #region Thực hiện post các biến thể nếu là sản phẩm biến thể
                if (product.type == ProductType.Variable && wcProduct != null)
                {
                    var productVariationList = _service.getProductVariationByProductID(productID);

                    foreach (var productVariation in productVariationList)
                    {
                        await _postWCVariation(productVariation, wcProduct, wcObject, checkHeader.priceType);
                    }
                }
                #endregion

                return Ok(wcProduct);
            }
            catch (WebException e)
            {
                var wcError = JsonConvert.DeserializeObject<WCErrorModel>(e.Message);

                return StatusCode(StatusCodes.Status500InternalServerError, wcError);
            }
            #endregion
        }

        /// <summary>
        /// Thực hiện post biến thể
        /// </summary>
        /// <param name="productID"></param>
        /// <param name="variationSKU"></param>
        /// <param name="wcProductID"></param>
        /// <returns></returns>
        [HttpPost]
        [Produces("application/json")]
        [Route("product/{productID:int}/variation/{variationSKU}")]
        public async Task<IActionResult> postProductVariation(int productID, string variationSKU, [FromBody]PostProductVariationParameter parameters)
        {
            #region Kiểm tra điều kiện header request
            var checkHeader = _checkHeaderRequest(Request.Headers);

            if (!checkHeader.success)
                return StatusCode(checkHeader.statusCode, checkHeader.message);

            var wcObject = checkHeader.wc.wcObject;
            #endregion

            #region Kiểm tra xem biến thể có tồn database gốc không
            var productVariation = _service.getProductVariationByProductID(productID, variationSKU);

            if (productVariation == null)
                return BadRequest(new ResponseModel() { success = false, message = "Không tìm thấy biến thể" });
            #endregion

            #region Kiểm tra xem có biến thể cha ở WooCommerce không
            //Get products
            var wcProduct = await wcObject.Product.Get(parameters.wcProductID);

            if (wcProduct == null)
                return BadRequest(new ResponseModel() { success = false, message = "Không tìm thấy sản phẩm cha " + parameters.wcProductID });
            #endregion
           
            #region Thức thi post sản phẩm biến thể
            //Add new product variation
            Variation wcProductVariation = await _postWCVariation(productVariation, wcProduct, wcObject, checkHeader.priceType);
            #endregion

            return Ok(wcProductVariation);
        }
        #endregion
        #region Get product
        /// <summary>
        /// Thực hiện get product
        /// </summary>
        /// <param name="productID"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("product/{productID:int}")]
        public async Task<IActionResult> getProduct(int productID)
        {
            #region Kiểm tra điều kiện header request
            var checkHeader = _checkHeaderRequest(Request.Headers);
            if (!checkHeader.success)
                return StatusCode(checkHeader.statusCode, checkHeader.message);
            var wcObject = checkHeader.wc.wcObject;
            #endregion

            #region Kiểm tra tồn tại sản phẩm trong data gốc
            var product = _service.getProductByID(productID);
            if (product == null)
                return BadRequest(new ResponseModel() { success = false, message = "Không tìm thấy sản phẩm trên hệ thống gốc" });
            #endregion

            #region Thực hiện get sản phẩm trên web
            var wcProduct = await wcObject.Product.GetAll(new Dictionary<string, string>() { { "sku", product.sku } });
            #endregion

            return Ok(wcProduct);
        }
        #endregion
        #region Up to the top
        /// <summary>
        /// Thực hiện up top product
        /// </summary>
        /// <param name="productID"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("product/{productID:int}/uptop")]
        public async Task<IActionResult> upTopProduct(int productID)
        {
            #region Kiểm tra điều kiện header request
            var checkHeader = _checkHeaderRequest(Request.Headers);
            if (!checkHeader.success)
                return StatusCode(checkHeader.statusCode, checkHeader.message);
            var wcObject = checkHeader.wc.wcObject;
            #endregion

            #region Kiểm tra tồn tại sản phẩm trong data gốc
            var product = _service.getProductByID(productID);
            if (product == null)
                return BadRequest(new ResponseModel() { success = false, message = "Không tìm thấy sản phẩm trên hệ thống gốc" });
            #endregion

            #region Thực hiện get sản phẩm trên web
            var wcProduct = await wcObject.Product.GetAll(new Dictionary<string, string>() { { "sku", product.sku } });
            if (wcProduct.Count == 0)
            {
                return BadRequest(new ResponseModel() { success = false, message = "Không tìm thấy sản phẩm trên web" });
            }
            #endregion

            int wcProductID = wcProduct.Select(x => x.id).FirstOrDefault().Value;
            DateTime newTime = DateTime.Now.AddHours(-8);
            var updateProduct = await wcObject.Product.Update(wcProductID, new Product { date_created_gmt = newTime, date_modified_gmt = newTime });

            return Ok(updateProduct);
        }
        #endregion
        #region Toggle Product
        /// <summary>
        /// Thực hiện toggle product
        /// </summary>
        /// <param name="productID"></param>
        /// <param name="toggleProduct"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("product/{productID:int}/{toggleProduct}")]
        public async Task<IActionResult> toggleProduct(int productID, string toggleProduct)
        {
            #region Kiểm tra điều kiện header request
            var checkHeader = _checkHeaderRequest(Request.Headers);
            if (!checkHeader.success)
                return StatusCode(checkHeader.statusCode, checkHeader.message);
            var wcObject = checkHeader.wc.wcObject;
            #endregion

            #region Kiểm tra tồn tại sản phẩm trong data gốc
            var product = _service.getProductByID(productID);
            if (product == null)
                return BadRequest(new ResponseModel() { success = false, message = "Không tìm thấy sản phẩm trên hệ thống gốc" });
            #endregion

            #region Thực hiện get sản phẩm trên web
            var wcProduct = await wcObject.Product.GetAll(new Dictionary<string, string>() { { "sku", product.sku } });
            if (wcProduct.Count == 0)
            {
                return BadRequest(new ResponseModel() { success = false, message = "Không tìm thấy sản phẩm trên web" });
            }
            #endregion

            int wcProductID = wcProduct.Select(x => x.id).FirstOrDefault().Value;
            var updateProduct = await wcObject.Product.Update(wcProductID, new Product { catalog_visibility = (toggleProduct == "show" ? "visible" : "search") });

            return Ok(updateProduct);
        }
        #endregion
        #region Update SKU
        /// <summary>
        /// Thực hiện update SKU
        /// </summary>
        /// <param name="oldSKU"></param>
        /// <param name="newSKU"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("product/updateSKU/{oldSKU}/{newSKU}")]
        public async Task<IActionResult> updateSKU(string oldSKU, string newSKU)
        {
            #region Kiểm tra điều kiện header request
            var checkHeader = _checkHeaderRequest(Request.Headers);
            if (!checkHeader.success)
                return StatusCode(checkHeader.statusCode, checkHeader.message);
            var wcObject = checkHeader.wc.wcObject;
            #endregion

            #region Thực hiện get sản phẩm trên web bằng SKU cũ
            var wcProduct = await wcObject.Product.GetAll(new Dictionary<string, string>() { { "sku", oldSKU } });
            if (wcProduct.Count == 0)
            {
                return BadRequest(new ResponseModel() { success = false, message = "Không tìm thấy sản phẩm trên web" });
            }
            #endregion

            int wcProductID = wcProduct.Select(x => x.id).FirstOrDefault().Value;
            var updateProduct = await wcObject.Product.Update(wcProductID, new Product { sku = newSKU });

            return Ok(updateProduct);
        }
        #endregion
        #region Update Retail Price
        /// <summary>
        /// Thực hiện update SKU
        /// </summary>
        /// <param name="productID"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("product/updateRetailPrice/{productID:int}")]
        public async Task<IActionResult> updateRetailPrice(int productID)
        {
            #region Kiểm tra điều kiện header request
            var checkHeader = _checkHeaderRequest(Request.Headers);
            if (!checkHeader.success)
                return StatusCode(checkHeader.statusCode, checkHeader.message);
            var wcObject = checkHeader.wc.wcObject;
            #endregion

            #region Kiểm tra tồn tại sản phẩm trong data gốc
            var product = _service.getProductByID(productID);
            if (product == null)
                return BadRequest(new ResponseModel() { success = false, message = "Không tìm thấy sản phẩm trên hệ thống gốc" });
            #endregion

            #region Thực hiện get sản phẩm trên web bằng SKU cũ
            var wcProduct = await wcObject.Product.GetAll(new Dictionary<string, string>() { { "sku", product.sku } });
            if (wcProduct.Count == 0)
            {
                return BadRequest(new ResponseModel() { success = false, message = "Không tìm thấy sản phẩm trên web" });
            }
            #endregion

            int wcProductID = wcProduct.Select(x => x.id).FirstOrDefault().Value;
            var updateProduct = await wcObject.Product.Update(wcProductID, new Product {
                meta_data = new List<WooCommerceNET.WooCommerce.v2.ProductMeta>()
                            {
                                new WooCommerceNET.WooCommerce.v2.ProductMeta()
                                {
                                    key = "_retail_price",
                                    value = product.retailPrice
                                }
                            }
            });

            return Ok(updateProduct);
        }
        #endregion
        #region Update Retail Price
        /// <summary>
        /// Thực hiện update SKU
        /// </summary>
        /// <param name="productID"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("product/updateWholesalePrice/{productID:int}")]
        public async Task<IActionResult> updateWholesalePrice(int productID)
        {
            #region Kiểm tra điều kiện header request
            var checkHeader = _checkHeaderRequest(Request.Headers);
            if (!checkHeader.success)
                return StatusCode(checkHeader.statusCode, checkHeader.message);
            var wcObject = checkHeader.wc.wcObject;
            #endregion

            #region Kiểm tra tồn tại sản phẩm trong data gốc
            var product = _service.getProductByID(productID);
            if (product == null)
                return BadRequest(new ResponseModel() { success = false, message = "Không tìm thấy sản phẩm trên hệ thống gốc" });
            #endregion

            #region Thực hiện get sản phẩm trên web bằng SKU cũ
            var wcProduct = await wcObject.Product.GetAll(new Dictionary<string, string>() { { "sku", product.sku } });
            if (wcProduct.Count == 0)
            {
                return BadRequest(new ResponseModel() { success = false, message = "Không tìm thấy sản phẩm trên web" });
            }
            #endregion

            int wcProductID = wcProduct.Select(x => x.id).FirstOrDefault().Value;
            var updateProduct = await wcObject.Product.Update(wcProductID, new Product
            {
                regular_price = Convert.ToDecimal(product.regularPrice)
            });

            return Ok(updateProduct);
        }
        #endregion
        #region Update Product Tag
        /// <summary>
        /// Thực hiện update SKU
        /// </summary>
        /// <param name="productID"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("product/updateProductTag/{productID:int}")]
        public async Task<IActionResult> updateProductTag(int productID)
        {
            #region Kiểm tra điều kiện header request
            var checkHeader = _checkHeaderRequest(Request.Headers);
            if (!checkHeader.success)
                return StatusCode(checkHeader.statusCode, checkHeader.message);
            var wcObject = checkHeader.wc.wcObject;
            #endregion

            #region Kiểm tra tồn tại sản phẩm trong data gốc
            var product = _service.getProductByID(productID);
            if (product == null)
                return BadRequest(new ResponseModel() { success = false, message = "Không tìm thấy sản phẩm trên hệ thống gốc" });
            #endregion

            #region Thực hiện get sản phẩm trên web bằng SKU cũ
            var wcProduct = await wcObject.Product.GetAll(new Dictionary<string, string>() { { "sku", product.sku } });
            if (wcProduct.Count == 0)
            {
                return BadRequest(new ResponseModel() { success = false, message = "Không tìm thấy sản phẩm trên web" });
            }
            #endregion

            var oldTags = wcProduct.FirstOrDefault().tags;

            var tags = new List<ProductTagLine>();
            foreach (var tag in product.tags)
            {
                if (tag.name == "hot")
                {
                    continue;
                }

                var wcTags = await wcObject.Tag.GetAll(new Dictionary<string, string>()
                {
                    {"per_page", "50"},
                    {"search", tag.name}
                });

                // tìm thấy trên WP
                if (wcTags != null && wcTags.Count > 0)
                {
                    var wcTag = wcTags.Where(x => x.name == tag.name).FirstOrDefault();

                    if (wcTag != null)
                    {
                        tags.Add(new ProductTagLine() { id = wcTag.id });
                        continue;
                    }

                }

                var wcTagNew = await _createWCProductTag(wcObject, tag.name);

                if (wcTagNew != null)
                    tags.Add(new ProductTagLine() { id = wcTagNew.id });
            }

            // Trộn tags WP và tags hệ thống gốc sau đó lọc duplicate
            var allTags = tags.Concat(oldTags).Distinct().ToList();

            int wcProductID = wcProduct.Select(x => x.id).FirstOrDefault().Value;
            var updateProduct = await wcObject.Product.Update(wcProductID, new Product
            {
                tags = allTags
            });

            return Ok(updateProduct);
        }
        #endregion
        #region Renew product
        /// <summary>
        /// Thực hiện renew product
        /// </summary>
        /// <param name="productID"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("product/{productID:int}/renew")]
        public async Task<IActionResult> renewProduct(int productID)
        {
            #region Kiểm tra điều kiện header request
            var checkHeader = _checkHeaderRequest(Request.Headers);
            if (!checkHeader.success)
                return StatusCode(checkHeader.statusCode, checkHeader.message);
            var wcObject = checkHeader.wc.wcObject;
            #endregion

            #region Kiểm tra tồn tại sản phẩm trong data gốc
            var product = _service.getProductByID(productID);
            if (product == null)
                return BadRequest(new ResponseModel() { success = false, message = "Không tìm thấy sản phẩm" });
            #endregion

            #region Thực hiện get sản phẩm trên web
            var getWCProduct = await wcObject.Product.GetAll(new Dictionary<string, string>() { { "sku", product.sku } });
            if (getWCProduct.Count == 0)
            {
                return BadRequest(new ResponseModel() { success = false, message = "Không tìm thấy sản phẩm trên web" });
            }
            #endregion

            #region Thực hiện update sản phẩm
            try
            {
                #region Lấy product ID trên web
                int wcProductID = getWCProduct.Select(x => x.id).FirstOrDefault().Value;
                #endregion

                #region Thực hiện xóa tất cả biến thể cũ nếu là sản phẩm biến thể
                string wcProductType = getWCProduct.Select(x => x.type).FirstOrDefault();
                if (wcProductType == ProductType.Variable)
                {
                    var wcProductVariationList = await wcObject.Product.Variations.GetAll(wcProductID);
                    foreach (var productVariation in wcProductVariationList)
                    {
                        await wcObject.Product.Variations.Delete(productVariation.id.Value, wcProductID, true);
                    }
                }
                #endregion

                #region Update sản phẩm
                Product updateProduct = await _handleWCProduct(product, wcObject, checkHeader.priceType, checkHeader.domain);
                Product wcProduct = await wcObject.Product.Update(wcProductID, updateProduct);
                #endregion

                #region Update hình trong nội dung sản phẩm
                string productContent = wcProduct.description + "<h3>" + product.name + "</h3>";
                wcProduct.images.RemoveAt(0);
                foreach (var item in wcProduct.images)
                {
                    productContent += String.Format("<p><img src='/wp-content/uploads/{0}' alt='{1}' /></p>", System.IO.Path.GetFileName(item.src), product.name);
                }
                var upProduct = await wcObject.Product.Update(wcProduct.id.Value, new Product { description = productContent });
                #endregion

                #region Thực hiện post các biến thể nếu là sản phẩm biến thể
                if (product.type == ProductType.Variable && wcProduct != null)
                {
                    var productVariationList = _service.getProductVariationByProductID(productID);
                    foreach (var productVariation in productVariationList)
                    {
                        await _postWCVariation(productVariation, wcProduct, wcObject, checkHeader.priceType);
                    }
                }
                #endregion

                return Ok(wcProduct);
            }
            catch (WebException e)
            {
                var wcError = JsonConvert.DeserializeObject<WCErrorModel>(e.Message);

                return StatusCode(StatusCodes.Status500InternalServerError, wcError);
            }
            #endregion
        }
        #endregion
        #region Delete Product
        /// <summary>
        /// Thực hiện delete product
        /// </summary>
        /// <param name="productID"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("product/{productID:int}/delete")]
        public async Task<IActionResult> deleteProduct(int productID)
        {
            #region Kiểm tra điều kiện header request
            var checkHeader = _checkHeaderRequest(Request.Headers);
            if (!checkHeader.success)
                return StatusCode(checkHeader.statusCode, checkHeader.message);
            var wcObject = checkHeader.wc.wcObject;
            #endregion

            #region Kiểm tra tồn tại sản phẩm trong data gốc
            var product = _service.getProductByID(productID);
            if (product == null)
                return BadRequest(new ResponseModel() { success = false, message = "Không tìm thấy sản phẩm trên hệ thống gốc" });
            #endregion

            #region Thực hiện get sản phẩm trên web
            var wcProduct = await wcObject.Product.GetAll(new Dictionary<string, string>() { { "sku", product.sku } });
            if (wcProduct.Count == 0)
            {
                return BadRequest(new ResponseModel() { success = false, message = "Không tìm thấy sản phẩm trên web" });
            }
            #endregion

            int wcProductID = wcProduct.Select(x => x.id).FirstOrDefault().Value;
            var updateProduct = await wcObject.Product.Delete(wcProductID, true);

            return Ok(updateProduct);
        }
        #endregion
        #endregion
    }
}
