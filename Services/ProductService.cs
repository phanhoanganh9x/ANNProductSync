using ANNwpsync.Models;
using ANNwpsync.Models.Common;
using ANNwpsync.Models.SQLServer;
using ANNwpsync.Services.FactoryPattern;
using ANNwpsync.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ANNwpsync.Services
{
    public class ProductService: IANNService
    {
        protected readonly ProductCategoryService _category = ANNFactoryService.getInstance<ProductCategoryService>();
        protected readonly StockService _stock = ANNFactoryService.getInstance<StockService>();

        #region Lấy danh sách sản phẩm
        #region Lấy thông tin biến thể màu 
        /// <summary>
        /// Lấy thông tin biến thể màu
        /// </summary>
        /// <param name="productIDs"></param>
        /// <returns></returns>
        private List<ProductColorModel> getColors(List<int> productIDs)
        {
            using (var con = new SQLServerContext())
            {
                var variables = con.tbl_Product.Where(x => productIDs.Contains(x.ID))
                    .Join(
                        con.tbl_ProductVariable,
                        p => p.ID,
                        v => v.ProductID,
                        (p, v) => new { productID = p.ID, productVariableID = v.ID }
                    )
                    .Join(
                        con.tbl_ProductVariableValue,
                        p => p.productVariableID,
                        v => v.ProductVariableID,
                        (p, v) => new
                        {
                            productID = p.productID,
                            variableValueID = v.VariableValueID.HasValue ? v.VariableValueID.Value : 0
                        }
                    )
                    .GroupBy(x => new { x.productID, x.variableValueID })
                    .Select(x => new { productID = x.Key.productID, variableValueID = x.Key.variableValueID })
                    .Join(
                        con.tbl_VariableValue.Where(x => x.VariableID == (int)VariableType.Color),
                        p => p.variableValueID,
                        v => v.ID,
                        (p, v) => new ProductColorModel()
                        {
                            productID = p.productID,
                            id = v.ID,
                            name = v.VariableValue
                        }
                    )
                    .OrderBy(o => o.id)
                    .ToList();

                return variables;
            }
        }
        #endregion

        #region Lấy thông tin biến size
        /// <summary>
        /// Lấy thông tin biến size
        /// </summary>
        /// <param name="productIDs"></param>
        /// <returns></returns>
        private List<ProductSizeModel> getSizes(List<int> productIDs)
        {
            using (var con = new SQLServerContext())
            {
                var variables = con.tbl_Product.Where(x => productIDs.Contains(x.ID))
                    .Join(
                        con.tbl_ProductVariable,
                        p => p.ID,
                        v => v.ProductID,
                        (p, v) => new { productID = p.ID, productVariableID = v.ID }
                    )
                    .Join(
                        con.tbl_ProductVariableValue,
                        p => p.productVariableID,
                        v => v.ProductVariableID,
                        (p, v) => new
                        {
                            productID = p.productID,
                            variableValueID = v.VariableValueID.HasValue ? v.VariableValueID.Value : 0
                        }
                    )
                    .GroupBy(x => new { x.productID, x.variableValueID })
                    .Select(x => new { productID = x.Key.productID, variableValueID = x.Key.variableValueID })
                    .Join(
                        con.tbl_VariableValue.Where(x => x.VariableID == (int)VariableType.Size),
                        p => p.variableValueID,
                        v => v.ID,
                        (p, v) => new ProductSizeModel()
                        {
                            productID = p.productID,
                            id = v.ID,
                            name = v.VariableValue
                        }
                    )
                    .OrderBy(o => o.id)
                    .ToList();

                return variables;
            }
        }
        #endregion

        #region Lấy danh sách sản phẩm theo điều kiện filter
        public List<ProductCardModel> getProducts(ProductFilterModel filter, ref PaginationMetadataModel pagination)
        {
            using (var con = new SQLServerContext())
            {
                var source = con.tbl_Product
                    .Select(x => new
                    {
                        categoryID = x.CategoryID.HasValue ? x.CategoryID.Value : 0,
                        productID = x.ID,
                        sku = x.ProductSKU,
                        title = x.ProductTitle,
                        unSignedTitle = x.UnSignedTitle,
                        slug = x.Slug,
                        materials = x.Materials,
                        preOrder = x.PreOrder,
                        availability = false,
                        avatar = x.ProductImage,
                        regularPrice = x.Regular_Price.HasValue ? x.Regular_Price.Value : 0,
                        oldPrice = x.Old_Price.HasValue ? x.Old_Price.Value : 0,
                        retailPrice = x.Retail_Price.HasValue ? x.Retail_Price.Value : 0,
                        content = x.ProductContent,
                        webPublish = x.WebPublish.HasValue ? x.WebPublish.Value : false,
                        webUpdate = x.WebUpdate,
                    });

                #region Lọc sản phẩm
                #region Lọc sản phẩm theo text search
                if (!String.IsNullOrEmpty(filter.productSearch))
                {
                    source = source
                        .Where(x =>
                            (
                                (
                                    x.sku.Trim().Length >= filter.productSearch.Trim().Length &&
                                    x.sku.Trim().ToLower().StartsWith(filter.productSearch.Trim().ToLower())
                                ) ||
                                (
                                    x.sku.Trim().Length < filter.productSearch.Trim().Length &&
                                    filter.productSearch.Trim().ToLower().StartsWith(x.sku.Trim().ToLower())
                                )
                            ) ||
                            x.title.Trim().ToLower().Contains(filter.productSearch.Trim().ToLower()) ||
                            x.unSignedTitle.Trim().ToLower().Contains(filter.productSearch.Trim().ToLower())
                        );
                }
                else
                {
                    // Trường hợp không phải là search thì kiểm tra điều web public
                    source = source.Where(x => x.webPublish == true);
                }
                #endregion

                #region Lọc sản phẩm theo tag slug
                if (!String.IsNullOrEmpty(filter.tagSlug))
                {
                    var tags = con.Tag.Where(x => x.Slug == filter.tagSlug.Trim().ToLower());
                    var prodTags = con.ProductTag
                        .Join(
                            tags,
                            pt => pt.TagID,
                            t => t.ID,
                            (pt, t) => pt
                        );

                    source = source
                        .Join(
                            prodTags,
                            p => p.productID,
                            t => t.ProductID,
                            (p, t) => p
                        );
                }
                #endregion

                #region Lấy theo preOrder (hang-co-san | hang-order)
                if (!String.IsNullOrEmpty(filter.productBadge))
                {
                    switch (filter.productBadge)
                    {
                        case "hang-co-san":
                            source = source.Where(x => x.preOrder == false);
                            break;
                        case "hang-order":
                            source = source.Where(x => x.preOrder == true);
                            break;
                        case "hang-sale":
                            source = source.Where(x => x.oldPrice > 0);
                            break;
                        default:
                            break;
                    }
                }
                #endregion

                #region Lấy theo wholesale price
                if (filter.priceMin > 0)
                {
                    source = source.Where(x => x.regularPrice >= filter.priceMin);
                }
                if (filter.priceMax > 0)
                {
                    source = source.Where(x => x.regularPrice <= filter.priceMax);
                }
                #endregion

                #region Lấy theo category slug
                if (!String.IsNullOrEmpty(filter.categorySlug))
                {
                    var categories = _category.getCategoryChild(filter.categorySlug);

                    if (categories == null || categories.Count == 0)
                        return null;

                    var categoryIDs = categories.Select(x => x.id).OrderByDescending(o => o).ToList();
                    source = source.Where(x => categoryIDs.Contains(x.categoryID));
                }
                #endregion

                #region Lấy theo category slug
                if (filter.categorySlugList != null && filter.categorySlugList.Count > 0)
                {
                    var categories = new List<ProductCategoryModel>();

                    foreach (var categorySlug in filter.categorySlugList)
                    {
                        var categoryChilds = _category.getCategoryChild(categorySlug);

                        if (categoryChilds == null || categoryChilds.Count == 0)
                            continue;

                        categories.AddRange(categoryChilds);
                    }

                    if (categories == null || categories.Count == 0)
                        return null;

                    var categoryIDs = categories.Select(x => x.id).Distinct().OrderByDescending(o => o).ToList();
                    source = source.Where(x => categoryIDs.Contains(x.categoryID));
                }
                #endregion

                #region Lấy thông tin sản phẩm và stock
                var stockFilter = con.tbl_StockManager
                    .Join(
                        source,
                        s => s.ParentID,
                        d => d.productID,
                        (s, d) => s
                    )
                    .ToList();
                var stocks = _stock.getQuantities(stockFilter);
                #endregion

                #region Lấy sản phẩm đạt yêu cầu
                var data = source
                    .ToList()
                    .GroupJoin(
                        stocks,
                        pro => pro.productID,
                        info => info.productID,
                        (pro, info) => new { pro, info }
                    )
                    .SelectMany(
                        x => x.info.DefaultIfEmpty(),
                        (parent, child) => new { product = parent.pro, stock = child }
                    )
                    .Select(x => new { x.product, x.stock });

                // Trường hợp không phải là search thì kiểm tra điều kiện stock
                if (String.IsNullOrEmpty(filter.productSearch))
                {
                    data = data.Where(x =>
                        x.product.preOrder ||
                        x.stock == null ||
                        (
                            x.stock != null &&
                            x.stock.quantity >= (x.product.categoryID == 44 ? 1 : 3)
                        )

                    );
                }
                #endregion
                #endregion

                #region Thực hiện sắp xếp sản phẩm
                if (filter.productSort == (int)ProductSortKind.PriceAsc)
                {
                    data = data.OrderBy(o => o.product.regularPrice);
                }
                else if (filter.productSort == (int)ProductSortKind.PriceDesc)
                {
                    data = data.OrderByDescending(o => o.product.regularPrice);
                }
                else if (filter.productSort == (int)ProductSortKind.ModelNew)
                {
                    data = data.OrderByDescending(o => o.product.productID);
                }
                else if (filter.productSort == (int)ProductSortKind.ProductNew)
                {
                    data = data.OrderByDescending(o => o.product.webUpdate);
                }
                else
                {
                    data = data.OrderByDescending(o => o.product.webUpdate);
                }
                #endregion

                #region Thực hiện phân trang
                // Lấy tổng số record sản phẩm
                pagination.totalCount = data.Count();

                // Calculating Totalpage by Dividing (No of Records / Pagesize)
                pagination.totalPages = (int)Math.Ceiling(pagination.totalCount / (double)pagination.pageSize);

                // Returns List of product after applying Paging
                var result = data
                    .Select(x => new ProductCardModel()
                    {
                        productID = x.product.productID,
                        sku = x.product.sku,
                        name = x.product.title,
                        slug = x.product.slug,
                        materials = x.product.materials,
                        badge = x.stock == null ? ProductBadge.warehousing :
                            (x.product.oldPrice > 0 ? ProductBadge.sale :
                                (x.product.preOrder ? ProductBadge.order :
                                    (x.stock.availability ? ProductBadge.stockIn : ProductBadge.stockOut))),
                        availability = x.stock != null ? x.stock.availability : x.product.availability,
                        thumbnails = Thumbnail.getALL(x.product.avatar),
                        regularPrice = x.product.regularPrice,
                        oldPrice = x.product.oldPrice,
                        retailPrice = x.product.retailPrice,
                        content = x.product.content
                    })
                    .Skip((pagination.currentPage - 1) * pagination.pageSize)
                    .Take(pagination.pageSize)
                    .ToList();

                // if CurrentPage is greater than 1 means it has previousPage
                pagination.previousPage = pagination.currentPage > 1 ? "Yes" : "No";

                // if TotalPages is greater than CurrentPage means it has nextPage
                pagination.nextPage = pagination.currentPage < pagination.totalPages ? "Yes" : "No";
                #endregion

                #region Lấy thông tin variable
                var colors = getColors(result.Select(x => x.productID).ToList());
                var sizes = getSizes(result.Select(x => x.productID).ToList());

                foreach (var prod in result)
                {
                    prod.colors = colors.Where(x => x.productID == prod.productID).ToList();
                    prod.sizes = sizes.Where(x => x.productID == prod.productID).ToList();
                }
                #endregion

                return result;
            }
        }
        #endregion
        #endregion

        #region Lấy thông tin sản phẩm
        #region Lấy danh sách màu của sản phẩm
        /// <summary>
        /// Lấy thông tin biến thể màu
        /// </summary>
        /// <param name="productID"></param>
        /// <returns></returns>
        public List<ProductColorModel> getColors(int productID)
        {
            return getColors(new List<int> { productID });
        }

        /// <summary>
        /// Lấy thông tin biến thể màu
        /// </summary>
        /// <param name="productID"></param>
        /// <param name="productVariationID"></param>
        /// <returns></returns>
        public string getColor(int productID, int productVariationID)
        {
            using (var con = new SQLServerContext())
            {
                var variations = con.tbl_ProductVariable
                    .Where(x => x.ProductID == productID)
                    .Where(x => x.ID == productVariationID)
                    .FirstOrDefault();

                if (variations == null)
                    return String.Empty;

                var color = con.tbl_ProductVariableValue
                    .Where(x => x.ProductVariableID == variations.ID)
                    .Join(
                        con.tbl_VariableValue.Where(x => x.VariableID == (int)VariableType.Color),
                        p => p.VariableValueID,
                        c => c.ID,
                        (p, c) => c
                    )
                    .FirstOrDefault();

                return color == null ? String.Empty : color.VariableValue;
            }
        }
        #endregion

        #region Lấy danh sách size của sản phẩm
        /// <summary>
        /// Lấy thông tin biến size
        /// </summary>
        /// <param name="productID"></param>
        /// <returns></returns>
        public List<ProductSizeModel> getSizes(int productID)
        {
            return getSizes(new List<int> { productID });
        }

        /// <summary>
        /// Lấy thông tin biến size
        /// </summary>
        /// <param name="productID"></param>
        /// <param name="productVariationID"></param>
        /// <returns></returns>
        public string getSize(int productID, int productVariationID)
        {
            using (var con = new SQLServerContext())
            {
                var variations = con.tbl_ProductVariable
                    .Where(x => x.ProductID == productID)
                    .Where(x => x.ID == productVariationID)
                    .FirstOrDefault();

                if (variations == null)
                    return String.Empty;

                var size = con.tbl_ProductVariableValue
                    .Where(x => x.ProductVariableID == variations.ID)
                    .Join(
                        con.tbl_VariableValue.Where(x => x.VariableID == (int)VariableType.Size),
                        p => p.VariableValueID,
                        s => s.ID,
                        (p, s) => s
                    )
                    .FirstOrDefault();

                return size == null ? String.Empty : size.VariableValue;
            }
        }
        #endregion

        #region Lấy danh sách hình ảnh của sản phẩm
        /// <summary>
        /// Lấy danh sách hình ảnh của sản phẩm
        /// </summary>
        /// <param name="productID"></param>
        /// <returns></returns>
        public List<string> getImageListByProduct(int productID)
        {
            using (var con = new SQLServerContext())
            {
                // Lấy hình ảnh của sản phẩm cha
                var imageProduct = con.tbl_Product
                    .Where(x => x.ID == productID)
                    .Where(x => !String.IsNullOrEmpty(x.ProductImage))
                    .Select(x => new { image = x.ProductImage })
                    .ToList();
                // Lấy hình anh trong bảng image
                var imageSource = con.tbl_ProductImage.Where(x => x.ProductID == productID)
                    .Select(x => new { image = x.ProductImage })
                    .ToList();

                var images = imageProduct
                    .Union(imageSource)
                    .Select(x => x.image)
                    .Distinct()
                    .ToList();

                if (images.Count == 0)
                {
                    return new List<string>() { String.Empty };
                }
                else
                {
                    return images.Select(x => x).ToList();
                }
            }
        }
        #endregion

        #region Lấy danh sách tag của sản phẩm
        /// <summary>
        /// Lấy danh sách tag của sản phẩm
        /// </summary>
        /// <param name="productID"></param>
        /// <returns></returns>
        private List<ProductTagModel> getTagListByProduct(int productID)
        {
            using (var con = new SQLServerContext())
            {
                // Lấy hình ảnh của sản phẩm cha
                var tags = con.ProductTag
                    .Where(x => x.ProductID == productID)
                    .Where(x => x.ProductVariableID == 0)
                    .Join(
                        con.Tag,
                        pt => pt.TagID,
                        t => t.ID,
                        (pt, t) => new ProductTagModel()
                        {
                            id = t.ID,
                            name = t.Name,
                            slug = t.Slug
                        }
                    )
                    .ToList();

                return tags;
            }
        }
        #endregion

        #region Lấy thông tin sản phẩm theo SKU
        /// <summary>
        /// Lấy thông tin sản phẩm theo slug
        /// </summary>
        /// <param name="slug"></param>
        /// <returns></returns>
        public ProductModel getProductByID(int productID)
        {
            using (var con = new SQLServerContext())
            {
                // Kiểm tra có sản phẩm không
                var product = con.tbl_Product.Where(x => x.ID == productID).FirstOrDefault();
                if (product == null)
                    return null;

                return getProductBySlug(product.Slug);
            }
        }
        #endregion
        #region Lấy thông tin sản phẩm theo slug
        /// <summary>
        /// Lấy thông tin sản phẩm theo slug
        /// </summary>
        /// <param name="slug"></param>
        /// <returns></returns>
        public ProductModel getProductBySlug(string slug)
        {
            using (var con = new SQLServerContext())
            {
                // Kiểm tra có sản phẩm không
                var data = con.tbl_Product.Where(x => x.Slug == slug);

                if (data.FirstOrDefault() == null)
                    return null;

                // Lấy ID sản phẩm
                var id = data.FirstOrDefault().ID;

                // Get quantity
                var stockFilter = con.tbl_StockManager
                    .Join(
                        data,
                        s => s.ParentID,
                        d => d.ID,
                        (s, d) => s
                    )
                    //.OrderBy(x => new { x.ParentID.Value, x.ProductID, x.ProductVariableID })
                    .ToList();
                var stocks = _stock.getQuantities(stockFilter);

                // Get info variable
                var colors = getColors(id);
                var sizes = getSizes(id);

                // Get images of product
                var images = getImageListByProduct(id);

                // Get tags of product
                var tags = getTagListByProduct(id);

                // Xuất thông tin cơ bản của sản phẩm
                var products = data.Where(x => x.CategoryID.HasValue)
                    .Join(
                        con.tbl_Category,
                        pro => pro.CategoryID.Value,
                        cat => cat.ID,
                        (p, c) => new
                        {
                            id = p.ID,
                            categoryName = c.CategoryName,
                            categorySlug = c.Slug,
                            name = p.ProductTitle,
                            sku = p.ProductSKU,
                            avatar = p.ProductImage,
                            materials = p.Materials,
                            regularPrice = p.Regular_Price.HasValue ? p.Regular_Price.Value : 0,
                            oldPrice = p.Old_Price.HasValue ? p.Old_Price.Value : 0,
                            retailPrice = p.Retail_Price.HasValue ? p.Retail_Price.Value : 0,
                            content = p.ProductContent,
                            slug = p.Slug,
                            preOrder = p.PreOrder,
                            productStyle = p.ProductStyle.HasValue ? p.ProductStyle.Value : 1
                        }
                    )
                    .OrderBy(x => x.id)
                    .ToList();

                // Lấy tất cả thông tin về sản phẩm
                var result = products
                    .GroupJoin(
                        stocks,
                        pro => pro.id,
                        info => info.productID,
                        (pro, info) => new { pro, info }
                    )
                    .SelectMany(
                        x => x.info.DefaultIfEmpty(),
                        (parent, child) => new ProductModel()
                        {
                            id = parent.pro.id,
                            categorySlug = parent.pro.categorySlug,
                            categoryName = parent.pro.categoryName,
                            name = parent.pro.name,
                            sku = parent.pro.sku,
                            avatar = parent.pro.avatar,
                            thumbnails = Thumbnail.getALL(parent.pro.avatar),
                            materials = parent.pro.materials,
                            regularPrice = parent.pro.regularPrice,
                            oldPrice = parent.pro.oldPrice,
                            retailPrice = parent.pro.retailPrice,
                            content = parent.pro.content,
                            slug = parent.pro.slug,
                            images = images,
                            colors = colors,
                            sizes = sizes,
                            badge = child == null ? ProductBadge.warehousing :
                                (parent.pro.oldPrice > 0 ? ProductBadge.sale :
                                    (parent.pro.preOrder ? ProductBadge.order :
                                        (child.availability ? ProductBadge.stockIn : ProductBadge.stockOut))),
                            tags = tags,
                            type = parent.pro.productStyle == 1 ? "simple" : "variable",
                            manage_stock = parent.pro.productStyle == 1 ? true : false,
                            stock_quantity = parent.pro.productStyle == 1 ? (child != null ? child.quantity : 0) : 0
                        }
                    )
                    .OrderBy(o => o.id)
                    .ToList();

                return result.FirstOrDefault();
            }
        }
        #endregion

        #region Đặt tên cho sản phẩm con theo color - màu
        /// <summary>
        /// Đặt tên cho sản phẩm con theo color - màu
        /// </summary>
        /// <param name="color"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        private string getVariableName(string color, string size)
        {
            var strColor = !String.IsNullOrEmpty(color) ? String.Format("-{0}", color) : String.Empty;
            var strSize = !String.IsNullOrEmpty(size) ? String.Format("-{0}", size) : String.Empty;
            var result = String.Concat(strColor, strSize);

            if (!String.IsNullOrEmpty(result) && result.Length > 1)
            {
                return result.Substring(1);
            }
            else
            {
                return String.Empty;
            }
        }
        #endregion

        #region Lấy thông tin các sản phẩm con
        /// <summary>
        /// Lấy thông tin các sản phẩm con
        /// </summary>
        /// <param name="productID"></param>
        /// <returns></returns>
        public List<ProductVariationModel> getProductVariationByProductID(int productID)
        {
            using (var con = new SQLServerContext())
            {
                // Kiểm tra có sản phẩm không
                var product = con.tbl_Product.Where(x => x.ID == productID).FirstOrDefault();

                if (product == null)
                    return null;

                // Returns List of Customer after applying Paging
                var source = con.tbl_ProductVariable
                    .Where(x => x.ProductID == product.ID)
                    .OrderBy(o => o.ID);

                // Get quantity
                var stockFilter = con.tbl_StockManager
                    .Join(
                        source,
                        s => new { productID = s.ParentID.Value, productVariableID = s.ProductVariableID.Value },
                        d => new { productID = d.ProductID.Value, productVariableID = d.ID },
                        (s, d) => s
                    )
                    //.OrderBy(x => new { x.ParentID, x.ProductID, x.ProductVariableID })
                    .ToList();

                var stocks = _stock.getProductVariableQuantities(stockFilter);

                var productVariable = source
                    .Select(x => new
                    {
                        productID = x.ProductID.Value,
                        productVariableID = x.ID,
                        sku = x.SKU,
                        avatar = x.Image,
                        regular_price = x.Regular_Price.HasValue ? x.Regular_Price.Value : 0D,
                        retail_price = x.RetailPrice.HasValue ? x.RetailPrice : 0D
                    })
                    .ToList();

                var result = productVariable
                    .Select(x => {
                        var stock = stocks
                            .Where(s => s.productID == x.productID)
                            .Where(s => s.productVariableID == x.productVariableID)
                            .FirstOrDefault();
                        var color = getColor(x.productID, x.productVariableID);
                        var size = getSize(x.productID, x.productVariableID);

                        return new ProductVariationModel()
                        {
                            sku = x.sku,
                            regular_price = Convert.ToInt32(x.regular_price),
                            retail_price = Convert.ToInt32(x.retail_price),
                            stock_quantity = stock != null ? stock.quantity : 0,
                            image = x.avatar,
                            color = color,
                            size = size
                        };
                    })
                    .ToList();

                return result;
            }
        }

        public ProductVariationModel getProductVariationByProductID(int productID, string variationSKU)
        {
            using (var con = new SQLServerContext())
            {
                // Kiểm tra có sản phẩm không
                var product = con.tbl_Product.Where(x => x.ID == productID).FirstOrDefault();

                if (product == null)
                    return null;

                // Returns List of Customer after applying Paging
                var variation = con.tbl_ProductVariable
                    .Where(x => x.ProductID == product.ID)
                    .Where(x => x.SKU == variationSKU)
                    .FirstOrDefault();

                // Get quantity
                var stockFilter = con.tbl_StockManager
                    .Where(x => x.ParentID == variation.ProductID)
                    .Where(x => x.ProductVariableID == variation.ID)
                    .ToList();

                var stocks = _stock.getProductVariableQuantities(stockFilter).FirstOrDefault();

                var result = new ProductVariationModel()
                {
                    sku = variation.SKU,
                    regular_price = variation.Regular_Price.HasValue ? Convert.ToInt32(variation.Regular_Price.Value) : 0,
                    stock_quantity = stocks != null ? stocks.quantity : 0,
                    image = variation.Image,
                    color = getColor(variation.ProductID.Value, variation.ID),
                    size = getSize(variation.ProductID.Value, variation.ID)
                };

                return result;
            }
        }
        #endregion

        #region Trà về hình ảnh tưởng chưng cho biến thể
        /// <summary>
        /// Trà về hình ảnh tưởng chưng cho biến thể
        /// </summary>
        /// <param name="productID"></param>
        /// <param name="variables"></param>
        /// <returns></returns>
        public string getImageWithVariable(int productID, int color, int size)
        {
            if (color == 0 && size == 0)
                return null;

            using (var con = new SQLServerContext())
            {
                var productVariable = con.tbl_ProductVariable
                    .Where(x => x.ProductID == productID)
                    .Select(x => new
                    {
                        productVariableID = x.ID,
                        avatar = x.Image
                    });

                var colors = productVariable
                    .Join(
                        con.tbl_ProductVariableValue.Where(x => x.VariableValueID.Value == color),
                        p => p.productVariableID,
                        vv => vv.ProductVariableID.Value,
                        (p, vv) => new { productVariableID = p.productVariableID }
                    );

                var sizes = productVariable
                    .Join(
                        con.tbl_ProductVariableValue.Where(x => x.VariableValueID.Value == size),
                        p => p.productVariableID,
                        vv => vv.ProductVariableID.Value,
                        (p, vv) => new { productVariableID = p.productVariableID }
                    );

                var images = productVariable;

                if (color > 0)
                    images = images.Join(colors, i => i.productVariableID, c => c.productVariableID, (i, c) => i);
                if (size > 0)
                    images = images.Join(sizes, i => i.productVariableID, s => s.productVariableID, (i, s) => i);

                return images.ToList().Select(x => Thumbnail.getURL(x.avatar, Thumbnail.Size.Source)).FirstOrDefault();
            }
        }
        #endregion

        #region Lấy danh sách hình ảnh của sản phẩm dùng cho download image
        /// <summary>
        /// Lấy danh sách hình ảnh của sản phẩm dùng cho download image
        /// </summary>
        /// <param name="productID"></param>
        /// <returns></returns>
        public List<string> getAdvertisementImages(int productID)
        {
            using (var con = new SQLServerContext())
            {
                // Lấy hình ảnh của sản phẩm cha
                var imageProduct = con.tbl_Product
                    .Where(x => x.ID == productID)
                    .Where(x => !String.IsNullOrEmpty(x.ProductImage))
                    .Select(x => new { image = x.ProductImage })
                    .ToList();
                // Lấy hình ảnh đại diện của các sản phẩm con
                var imageProductVariable = con.tbl_ProductVariable
                    .Where(x => x.ProductID == productID)
                    .Where(x => !String.IsNullOrEmpty(x.Image))
                    .Select(x => new { image = x.Image })
                    .ToList();
                // Lấy hình anh trong bảng image
                var imageSource = con.tbl_ProductImage.Where(x => x.ProductID == productID)
                    .Select(x => new { image = x.ProductImage })
                    .ToList();

                var images = imageProduct
                    .Union(imageProductVariable)
                    .Union(imageSource)
                    .Select(x => x.image)
                    .Distinct()
                    .ToList();

                if (images.Count == 0)
                {
                    return null;
                }
                else
                {
                    return images.Select(x => Thumbnail.getURL(x, Thumbnail.Size.Source)).ToList();
                }
            }
        }
        #endregion
        #endregion
    }
}
