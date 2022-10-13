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
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
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
                    image.id = Convert.ToUInt32(wcImageID);
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
                uint colorID = color != null ? color.id.Value : 1;
                attributes.Add(new VariationAttribute()
                {
                    id = colorID,
                    option = productVariation.color,
                });
            }

            if (!String.IsNullOrEmpty(productVariation.size))
            {
                var size = await _getWCAttribute(wcObject, "Size");
                uint sizeID = size != null ? size.id.Value : 2;
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
            (int)wcProduct.id.Value);

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
                            addBefore = "Shop chuyên sỉ";
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
                            addBefore = "Kho chuyên sỉ";
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
                            addBefore = "Phân phối sỉ";
                            break;
                        case "9":
                            addBefore = "Giá sỉ";
                            break;
                        case "8":
                            addBefore = "Shop sỉ";
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
                            addBefore = "Nguồn sỉ";
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
                            addBefore = "Bỏ sỉ";
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
                            addBefore = "Kho sỉ";
                            break;
                        case "4":
                            addBefore = "Mua sỉ";
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
                            addBefore = "Shop sỉ";
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
                            addBefore = "Kho sỉ";
                            break;
                        case "1":
                            addBefore = "Nhập sỉ";
                            break;
                        case "4":
                            addBefore = "Chuyên sỉ";
                            break;
                        case "5":
                            addBefore = "Nguồn sỉ";
                            break;
                        case "7":
                            addBefore = "Bỏ sỉ";
                            break;
                        default:
                            break;
                    }
                    break;
                case "simyphamonline.com":
                    switch (ID)
                    {
                        case "0":
                            addBefore = "Đánh giá";
                            break;
                        case "1":
                            addBefore = "Bảng giá sỉ";
                            break;
                        case "4":
                            addBefore = "Phân phối";
                            break;
                        case "7":
                            addBefore = "Sỉ";
                            break;
                        default:
                            break;
                    }
                    break;
                case "khosimypham.com":
                    switch (ID)
                    {
                        case "2":
                            addBefore = "Kho sỉ";
                            break;
                        case "3":
                            addBefore = "Giá sỉ";
                            break;
                        case "8":
                            addBefore = "Sỉ";
                            break;
                        default:
                            break;
                    }
                    break;
                case "nguonmypham.com":
                    switch (ID)
                    {
                        case "9":
                            addBefore = "Nguồn sỉ";
                            break;
                        case "6":
                            addBefore = "Giá sỉ";
                            break;
                        default:
                            break;
                    }
                    break;
                case "myphamann.vn":
                    switch (ID)
                    {
                        case "5":
                            addBefore = "Phân phối sỉ";
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
        private async Task<Product> _handleWCProduct(ProductModel product, WCObject wcObject, string priceType, string domain, bool cleanName = false, bool featuredImage = false, string catalogVisibility = "visible")
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
            var strImages = new List<string>();
            var images = new List<ProductImage>();

            if (featuredImage == true && !String.IsNullOrEmpty(product.FeaturedImage))
            {
                strImages.Add(product.FeaturedImage);
            }
            else
            {
                if (!String.IsNullOrEmpty(product.avatar))
                    strImages.Add(product.avatar);
            }

            if (product.images.Any())
                strImages.AddRange(product.images);

            if (strImages.Any())
            {
                var index = 0;
                images = strImages
                    .Distinct()
                    .Select(x =>
                    {
                        var item = new ProductImage()
                        {
                            src = $"http://hethongann.com/uploads/images/{x}",
                            alt = product.name + " - " + product.sku,
                            position = index
                        };

                        ++index;

                        return item;
                    })
                    .ToList();

                // nếu post sản phẩm clean name thì đảo image
                //if (cleanName == true && images.Count > 1)
                //{
                //    if (images.Count == 2)
                //    {
                //        images = images.OrderBy(x => x.position).ToList();
                //    }
                //    else if (images.Count == 3)
                //    {
                //        images = images.Select(x =>
                //        {
                //            if (x.position == 0)
                //                x.position = 2;
                //            else if (x.position == 2)
                //                x.position = 0;

                //            return x;
                //        })
                //        .OrderBy(x => x.position)
                //        .ToList();
                //    }
                //    else if (images.Count == 4)
                //    {
                //        images = images.Select(x =>
                //        {
                //            if (x.position == 0)
                //                x.position = 3;
                //            else if (x.position == 3)
                //                x.position = 0;

                //            return x;
                //        })
                //        .OrderBy(x => x.position)
                //        .ToList();
                //    }
                //    else if (images.Count == 5)
                //    {
                //        images = images.Select(x =>
                //        {
                //            if (x.position == 0)
                //                x.position = 4;
                //            else if (x.position == 4)
                //                x.position = 0;

                //            return x;
                //        })
                //        .OrderBy(x => x.position)
                //        .ToList();
                //    }
                //    else if (images.Count == 6)
                //    {
                //        images = images.Select(x =>
                //        {
                //            if (x.position == 0)
                //                x.position = 5;
                //            else if (x.position == 5)
                //                x.position = 0;

                //            return x;
                //        })
                //        .OrderBy(x => x.position)
                //        .ToList();
                //    }
                //    else if (images.Count == 7)
                //    {
                //        images = images.Select(x =>
                //        {
                //            if (x.position == 0)
                //                x.position = 6;
                //            else if (x.position == 6)
                //                x.position = 0;

                //            return x;
                //        })
                //        .OrderBy(x => x.position)
                //        .ToList();
                //    }
                //    else if (images.Count == 8)
                //    {
                //        images = images.Select(x =>
                //        {
                //            if (x.position == 0)
                //                x.position = 7;
                //            else if (x.position == 7)
                //                x.position = 0;

                //            return x;
                //        })
                //        .OrderBy(x => x.position)
                //        .ToList();
                //    }
                //    else
                //    {
                //        images = images.Select(x =>
                //        {
                //            if (x.position == 0)
                //                x.position = 8;
                //            else if (x.position == 8)
                //                x.position = 0;

                //            return x;
                //        })
                //        .OrderBy(x => x.position)
                //        .ToList();
                //    }
                //}
            }
            #endregion

            #region Attribute List
            var attributes = new List<ProductAttributeLine>();
            if (product.type == ProductType.Variable)
            {
                if (product.colors.Count > 0)
                {
                    var color = await _getWCAttribute(wcObject, "Màu");
                    uint colorID = color != null ? color.id.Value : 1;

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
                    uint sizeID = size != null ? size.id.Value : 1;

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

            #region Product Name & SKU & catalog_visibility
            string productName = _renameProduct(domain, product.id, (domain == "myphamann.vn") ? product.CleanName : product.name);
            
            // xử lý name - SKU - visibility khi post sản phẩm clean name
            if (cleanName == true)
            {
                productName = product.name;

                if (!String.IsNullOrEmpty(product.CleanName))
                {
                    productName = product.CleanName;
                }
            }
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
                    productContent += String.Format("<iframe width='100%' height='360' src='https://www.youtube.com/embed/{0}?controls=0' title='{1}' frameborder='0' allow='accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture' allowfullscreen></iframe><br>", item.VideoId, product.name);
                }
            }

            // Materials
            string[] noMaterials = { "my-pham", "kem-face", "kem-body", "serum", "tam-trang", "sua-tam", "sua-rua-mat", "dau-goi-dau", "son-moi", "kem-chong-nang", "my-pham-tong-hop", "dung-dich-ve-sinh", "mat-na-duong-da", "nuoc-hoa", "nuoc-hoa-charme", "nuoc-hoa-lua", "nuoc-hoa-noi-dia-trung", "nuoc-hoa-vung-kin", "nuoc-hoa-mini", "nuoc-hoa-full-size", "bao-li-xi-tet", "thuc-pham-chuc-nang", "tong-hop", "nuoc-tay-trang", "kem-danh-rang", "kem-tan-mo" };
            if (!noMaterials.Contains(product.categorySlug))
            {
                productContent += "Chất liệu " + product.materials + ".<br><br>";
                shortDescription += "Chất liệu " + product.materials;
            }
            else
            {
                if (cleanName == true)
                {
                    shortDescription = "- Phân phối " + char.ToLower(product.name[0]) + product.name.Substring(1) + "<br>";
                    shortDescription += product.short_description;
                    //shortDescription = "<p>- Giá sỉ " + char.ToLower(product.name[0]) + product.name.Substring(1) + "</p>";
                    //shortDescription += "<p>- <strong>Cam kết hàng chính hãng 100%</strong>.</p>";
                    //shortDescription += "<p>- <strong>Thanh toán khi nhận hàng (COD)</strong>.</p>";
                    //shortDescription += "<p>- <strong>Khách được kiểm tra hàng</strong>.</p>";
                }
            }

            // Nếu post sản phẩm clean name thì lấy mô tả ngắn làm nội dung
            if (cleanName == true)
            {
                productContent += product.short_description + "<br>";
            }
            else
            {
                productContent += product.content + "<br>";
            }
            #endregion

            return new Product()
            {
                name = productName,
                sku = product.sku,
                regular_price = regular_price,
                type = product.type,
                description = HttpUtility.HtmlDecode(productContent),
                short_description = HttpUtility.HtmlDecode(shortDescription),
                categories = categories,
                tags = tags,
                images = images,
                attributes = attributes,
                manage_stock = product.manage_stock,
                stock_quantity = product.stock_quantity,
                catalog_visibility = catalogVisibility,
                meta_data = new List<WooCommerceNET.WooCommerce.v2.ProductMeta>()
                    {
                        new WooCommerceNET.WooCommerce.v2.ProductMeta()
                        {
                            key = "_retail_price",
                            value = product.retailPrice
                        },
                        new WooCommerceNET.WooCommerce.v2.ProductMeta()
                        {
                            key = "_price10",
                            value = product.Price10
                        },
                        new WooCommerceNET.WooCommerce.v2.ProductMeta()
                        {
                            key = "_bestprice",
                            value = product.BestPrice
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
            if (checkHeader.domain == "nguonmypham.com")
            {
                if (String.IsNullOrEmpty(product.FeaturedImage))
                {
                    return BadRequest(new ResponseModel() { success = false, message = "Không có hình ảnh đặc trưng" });
                }
                if (String.IsNullOrEmpty(product.CleanName))
                {
                    return BadRequest(new ResponseModel() { success = false, message = "Không có tên sản phẩm 2" });
                }
            }
            #region Thực hiện post sản phẩm
            try
            {
                //Add new product
                bool featuredImage = false;
                bool CleanName = false;
                string catalogVisibility = "visible";
                if (checkHeader.domain == "nguonmypham.com")
                {
                    featuredImage = true;
                    CleanName = true;
                }

                Product newProduct = await _handleWCProduct(product, wcObject, checkHeader.priceType, checkHeader.domain, CleanName, featuredImage, catalogVisibility);
                
                Product wcProduct = await wcObject.Product.Add(newProduct);

                if (checkHeader.domain != "myphamann.vn")
                {
                    #region Update hình trong nội dung sản phẩm
                    string productContent = wcProduct.description + "<h3>" + product.name + "</h3>";
                    // wcProduct.images.RemoveAt(0);
                    foreach (var item in wcProduct.images)
                    {
                        productContent += String.Format("<p><img src='/wp-content/uploads/{0}' alt='{1}' /></p>", System.IO.Path.GetFileName(item.src), product.name);
                    }
                    var updateProduct = await wcObject.Product.Update((int)wcProduct.id.Value, new Product { description = productContent });
                    #endregion
                }


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
        #region Post Product Clean Name
        /// <summary>
        /// Thực hiện post sản phẩm
        /// </summary>
        /// <param name="productID"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("product/{productID:int}/cleanname")]
        public async Task<IActionResult> postProductCleanName(int productID)
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
                Product newProduct = await _handleWCProduct(product, wcObject, checkHeader.priceType, checkHeader.domain, true);
                Product wcProduct = await wcObject.Product.Add(newProduct);

                #region Update hình trong nội dung sản phẩm
                string productContent = wcProduct.description + "<h2>Giá sỉ " + product.CleanName + "</h2>";
                // wcProduct.images.RemoveAt(0);
                wcProduct.images = wcProduct.images.OrderByDescending(x => x.position).ToList();
                foreach (var item in wcProduct.images)
                {
                    productContent += String.Format("<p><img src='/wp-content/uploads/{0}' alt='{1}' /></p>", System.IO.Path.GetFileName(item.src), product.CleanName);
                }
                var updateProduct = await wcObject.Product.Update((int)wcProduct.id.Value, new Product { description = productContent });
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
        #region Post Product 2
        /// <summary>
        /// Thực hiện post sản phẩm
        /// </summary>
        /// <param name="productID"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("product/{productID:int}/post-2")]
        public async Task<IActionResult> postProduct2(int productID)
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
            if (checkHeader.domain != "nguonmypham.com")
            {
                if (String.IsNullOrEmpty(product.FeaturedImage))
                {
                    return BadRequest(new ResponseModel() { success = false, message = "Không có hình ảnh đặc trưng" });
                }
                if (String.IsNullOrEmpty(product.CleanName))
                {
                    return BadRequest(new ResponseModel() { success = false, message = "Không có tên sản phẩm 2" });
                }
            }
            #region Thực hiện post sản phẩm
            try
            {
                //Add new product
                bool featuredImage = true;
                bool CleanName = true;
                string catalogVisibility = "hidden";
                if (checkHeader.domain == "nguonmypham.com")
                {
                    featuredImage = false;
                    CleanName = false;
                }

                Product newProduct = await _handleWCProduct(product, wcObject, checkHeader.priceType, checkHeader.domain, CleanName, featuredImage, catalogVisibility);
                Product wcProduct = await wcObject.Product.Add(newProduct);

                #region Update hình trong nội dung sản phẩm
                string productContent = wcProduct.description + "<h2>Tổng phân phối " + product.name + "</h2>";

                wcProduct.images = wcProduct.images.OrderByDescending(x => x.position).ToList();
                foreach (var item in wcProduct.images)
                {
                    productContent += String.Format("<p><img src='/wp-content/uploads/{0}' alt='{1}' /></p>", System.IO.Path.GetFileName(item.src), product.CleanName);
                }
                var updateProduct = await wcObject.Product.Update((int)wcProduct.id.Value, new Product { description = productContent });
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
        #region Get product by SKU
        /// <summary>
        /// Thực hiện get product
        /// </summary>
        /// <param name="productID"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("product/{SKU}")]
        public async Task<IActionResult> getProduct(string SKU)
        {
            #region Kiểm tra điều kiện header request
            var checkHeader = _checkHeaderRequest(Request.Headers);
            if (!checkHeader.success)
                return StatusCode(checkHeader.statusCode, checkHeader.message);
            var wcObject = checkHeader.wc.wcObject;
            #endregion

            #region Thực hiện get sản phẩm trên web
            var wcProduct = await wcObject.Product.GetAll(new Dictionary<string, string>() { { "sku", SKU } });
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

            var wcProductUpdate = new List<Product>();

            foreach (var item in wcProduct)
            {
                int wcProductID = (int)item.id.Value;
                DateTime newTime = DateTime.Now.AddHours(-8);

                try
                {
                    var data = await wcObject.Product.Update(wcProductID, new Product { date_created_gmt = newTime, date_modified_gmt = newTime });
                    
                    wcProductUpdate.Add(data);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                    continue;
                }
            }
            return Ok(wcProductUpdate);
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

            var wcProductUpdate = new List<Product>();

            foreach (var item in wcProduct)
            {
                int wcProductID = (int)item.id.Value;

                try
                {
                    var data = await wcObject.Product.Update(wcProductID, new Product { catalog_visibility = (toggleProduct == "show" ? "visible" : "search") });

                    wcProductUpdate.Add(data);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                    continue;
                }
            }
            return Ok(wcProductUpdate);
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

            var wcProductUpdate = new List<Product>();

            foreach (var item in wcProduct)
            {
                int wcProductID = (int)item.id.Value;

                try
                {
                    var data = await wcObject.Product.Update(wcProductID, new Product { sku = newSKU });

                    wcProductUpdate.Add(data);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                    continue;
                }
            }
            return Ok(wcProductUpdate);
        }
        #endregion
        #region Update Price
        /// <summary>
        /// Thực hiện update SKU
        /// </summary>
        /// <param name="productID"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("product/updatePrice/{productID:int}")]
        public async Task<IActionResult> updatePrice(int productID)
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
            #region Thực hiện get sản phẩm trên web bằng SKU
            var wcProduct = await wcObject.Product.GetAll(new Dictionary<string, string>() { { "sku", product.sku } });

            if (wcProduct.Count == 0)
            {
                return BadRequest(new ResponseModel() { success = false, message = "Không tìm thấy sản phẩm trên web" });
            }
            #endregion

            var wcProductUpdate = new List<Product>();

            foreach (var item in wcProduct)
            {
                int wcProductID = (int)item.id.Value;

                try
                {
                    var data = await wcObject.Product.Update(wcProductID, new Product
                    {
                        regular_price = Convert.ToDecimal(product.regularPrice),
                        meta_data = new List<WooCommerceNET.WooCommerce.v2.ProductMeta>()
                            {
                                new WooCommerceNET.WooCommerce.v2.ProductMeta()
                                {
                                    key = "_retail_price",
                                    value = product.retailPrice
                                },
                                new WooCommerceNET.WooCommerce.v2.ProductMeta()
                                {
                                    key = "_price10",
                                    value = product.Price10
                                },
                                new WooCommerceNET.WooCommerce.v2.ProductMeta()
                                {
                                    key = "_bestprice",
                                    value = product.BestPrice
                                }
                            }
                    });

                    wcProductUpdate.Add(data);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                    continue;
                }
            }
            return Ok(wcProductUpdate);
        }
        #endregion
        #region Update hidden wholesale price
        /// <summary>
        /// Thực hiện update SKU
        /// </summary>
        /// <param name="productID"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("product/toggleWholesalePrice/{productID:int}/{toggleProduct}")]
        public async Task<IActionResult> toggleWholesalePrice(int productID, string toggleProduct)
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

            double regularPrice = product.regularPrice;
            double retailPrice = product.retailPrice;
            double price10 = product.Price10;
            double bestPrice = product.BestPrice;
            string shortDescription = "";

            if (toggleProduct == "hide")
            {
                regularPrice = retailPrice;
                price10 = retailPrice;
                bestPrice = retailPrice;
                shortDescription = "<p><strong style='color: #008000;'>Kho Sỉ Mỹ Phẩm ANN là nhà phân phối mỹ phẩm chính hãng tại TPHCM.</strong></p>\r\n";
                shortDescription += "<p><strong style='color: #ff0000;'>Chúng tôi không công khai giá sỉ, vui lòng liên hệ để nhận giá sỉ siêu chiết khấu!</strong></p>\r\n\r\n";
            }
            shortDescription += product.short_description;

            var wcProductUpdate = new List<Product>();

            foreach (var item in wcProduct)
            {
                int wcProductID = (int)item.id.Value;

                try
                {
                    var data = await wcObject.Product.Update(wcProductID, new Product
                    {
                        short_description = HttpUtility.HtmlDecode(shortDescription),
                        regular_price = Convert.ToDecimal(regularPrice),
                        meta_data = new List<WooCommerceNET.WooCommerce.v2.ProductMeta>()
                                {
                                    new WooCommerceNET.WooCommerce.v2.ProductMeta()
                                    {
                                        key = "_retail_price",
                                        value = retailPrice
                                    },
                                    new WooCommerceNET.WooCommerce.v2.ProductMeta()
                                    {
                                        key = "_price10",
                                        value = price10
                                    },
                                    new WooCommerceNET.WooCommerce.v2.ProductMeta()
                                    {
                                        key = "_bestprice",
                                        value = bestPrice
                                    }
                                }
                    });

                    wcProductUpdate.Add(data);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                    continue;
                }
            }
            return Ok(wcProductUpdate);
        }
        #endregion
        #region Update hidden price type
        /// <summary>
        /// Thực hiện update SKU
        /// </summary>
        /// <param name="productID"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("product/togglePrice/{productID:int}/{toggleProduct}/{priceType}")]
        public async Task<IActionResult> togglePrice(int productID, string toggleProduct, string priceType)
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

            double regularPrice = product.regularPrice;
            double retailPrice = product.retailPrice;
            double price10 = product.Price10;
            double bestPrice = product.BestPrice;

            var wcProductUpdate = new List<Product>();

            foreach (var item in wcProduct)
            {
                int wcProductID = (int)item.id.Value;

                try
                {
                    var data = new Product();

                    if (priceType == "WholesalePrice")
                    {
                        data = await wcObject.Product.Update(wcProductID, new Product
                        {
                            regular_price = Convert.ToDecimal(toggleProduct == "hide" ? retailPrice : regularPrice)
                        });
                    }

                    if (priceType == "RetailPrice")
                    {
                        data = await wcObject.Product.Update(wcProductID, new Product
                        {
                            meta_data = new List<WooCommerceNET.WooCommerce.v2.ProductMeta>()
                                {
                                    new WooCommerceNET.WooCommerce.v2.ProductMeta()
                                    {
                                        key = "_retail_price",
                                        value = Convert.ToDecimal(toggleProduct == "hide" ? 0 : retailPrice)
                                    }
                                }
                        });
                    }

                    if (priceType == "Price10")
                    {
                        data = await wcObject.Product.Update(wcProductID, new Product
                        {
                            meta_data = new List<WooCommerceNET.WooCommerce.v2.ProductMeta>()
                                {
                                    new WooCommerceNET.WooCommerce.v2.ProductMeta()
                                    {
                                        key = "_price10",
                                        value = Convert.ToDecimal(toggleProduct == "hide" ? 0 : price10)
                                    }
                                }
                        });
                    }

                    if (priceType == "BestPrice")
                    {
                        data = await wcObject.Product.Update(wcProductID, new Product
                        {
                            regular_price = Convert.ToDecimal(regularPrice),
                            meta_data = new List<WooCommerceNET.WooCommerce.v2.ProductMeta>()
                                {
                                    new WooCommerceNET.WooCommerce.v2.ProductMeta()
                                    {
                                        key = "_bestprice",
                                        value = Convert.ToDecimal(toggleProduct == "hide" ? 0 : bestPrice)
                                    }
                                }
                        });
                    }

                    wcProductUpdate.Add(data);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                    continue;
                }
            }
            return Ok(wcProductUpdate);

        }
        #endregion
        #region Update Product Category
        /// <summary>
        /// Thực hiện update SKU
        /// </summary>
        /// <param name="productID"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("product/updateProductCategory/{productID:int}")]
        public async Task<IActionResult> updateProductCategory(int productID)
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

            #region Tag List
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

            var wcProductUpdate = new List<Product>();

            foreach (var item in wcProduct)
            {
                int wcProductID = (int)item.id.Value;

                try
                {
                    var data = await wcObject.Product.Update(wcProductID, new Product
                    {
                        tags = allTags,
                        categories = categories
                    });

                    wcProductUpdate.Add(data);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                    continue;
                }
            }
            return Ok(wcProductUpdate);
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

            var wcProductUpdate = new List<Product>();

            foreach (var item in wcProduct)
            {
                int wcProductID = (int)item.id.Value;

                try
                {
                    var data = await wcObject.Product.Update(wcProductID, new Product
                    {
                        tags = allTags
                    });

                    wcProductUpdate.Add(data);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                    continue;
                }
            }
            return Ok(wcProductUpdate);
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
            var getWCProduct = await wcObject.Product.GetAll(new Dictionary<string, string>() { { "sku", product.sku }, { "catalog_visibility", "visible" } });
            if (getWCProduct.Count == 0)
            {
                return BadRequest(new ResponseModel() { success = false, message = "Không tìm thấy sản phẩm trên web" });
            }
            #endregion

            #region Thực hiện update sản phẩm
            try
            {
                #region Lấy product ID trên web
                int wcProductID = (int)getWCProduct.Select(x => x.id).FirstOrDefault().Value;
                #endregion

                #region Thực hiện xóa tất cả biến thể cũ nếu là sản phẩm biến thể
                string wcProductType = getWCProduct.Select(x => x.type).FirstOrDefault();
                if (wcProductType == ProductType.Variable)
                {
                    var wcProductVariationList = await wcObject.Product.Variations.GetAll(wcProductID);
                    foreach (var productVariation in wcProductVariationList)
                    {
                        await wcObject.Product.Variations.Delete((int)productVariation.id.Value, wcProductID, true);
                    }
                }
                #endregion

                #region Update sản phẩm
                Product updateProduct = await _handleWCProduct(product, wcObject, checkHeader.priceType, checkHeader.domain);
                Product wcProduct = await wcObject.Product.Update(wcProductID, updateProduct);
                #endregion

                #region Update hình trong nội dung sản phẩm
                string productContent = wcProduct.description + "<h3>" + product.name + "</h3>";
                // wcProduct.images.RemoveAt(0);
                foreach (var item in wcProduct.images)
                {
                    productContent += String.Format("<p><img src='/wp-content/uploads/{0}' alt='{1}' /></p>", System.IO.Path.GetFileName(item.src), product.name);
                }
                var upProduct = await wcObject.Product.Update((int)wcProduct.id.Value, new Product { description = productContent });
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
        #region Change product category
        /// <summary>
        /// Thực hiện up top product
        /// </summary>
        /// <param name="productID"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("product/updateCategory/{productID:int}")]
        public async Task<IActionResult> changeProductCategory(int productID)
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

            #region Category List
            var categories = new List<ProductCategoryLine>();
            var wcProductCategory = await wcObject.Category.GetAll(new Dictionary<string, string>() { { "search", product.categoryName } });
            if (wcProductCategory.Count > 0)
            {
                var wcProductCategoryID = wcProductCategory.Select(x => x.id).FirstOrDefault();
                categories.Add(new ProductCategoryLine() { id = wcProductCategoryID });
            }
            #endregion

            var wcProductUpdate = new List<Product>();

            foreach (var item in wcProduct)
            {
                int wcProductID = (int)item.id.Value;

                try
                {
                    var data = await wcObject.Product.Update(wcProductID, new Product
                    {
                        categories = categories
                    });

                    wcProductUpdate.Add(data);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                    continue;
                }
            }
            return Ok(wcProductUpdate);
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
            foreach (var item in wcProduct)
            {
                int wcProductID = (int)item.id.Value;
                await wcObject.Product.Delete(wcProductID, true);
            }

            return Ok(new ResponseModel() { success = true, message = "OK" });
        }
        #endregion
        #endregion
    }
}
