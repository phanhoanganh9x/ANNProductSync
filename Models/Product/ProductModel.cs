using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ANNwpsync.Models
{
    public class ProductModel
    {
        public int id { get; set; }
        public string categoryName { get; set; }
        public string categorySlug { get; set; }
        public string name { get; set; }
        public string sku { get; set; }
        public string avatar { get; set; }
        public List<ThumbnailModel> thumbnails { get; set; }
        public string materials { get; set; }
        public double regularPrice { get; set; }
        public double oldPrice { get; set; }
        public double retailPrice { get; set; }
        public string content { get; set; }
        public string slug { get; set; }
        public List<string> images { get; set; }
        public List<ProductColorModel> colors { get; set; }
        public List<ProductSizeModel> sizes { get; set; }
        public int badge { get; set; }
        public List<ProductTagModel> tags { get; set; }
        public string type { get; set; }
        public bool manage_stock { get; set; }
        public int? stock_quantity { get; set; }
        public string short_description { get; set; }
        public double Price10 { get; set; }
        public double BestPrice { get; set; }
    }
}
