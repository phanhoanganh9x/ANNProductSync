using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ANNProductSync.Models
{
    public class ProductCardModel
    {
        public int productID { get; set; }
        public string sku { get; set; }
        public string name { get; set; }
        public string slug { get; set; }
        public string materials { get; set; }
        public List<ProductColorModel> colors { get; set; }
        public List<ProductSizeModel> sizes { get; set; }
        public int badge { get; set; }
        public bool availability { get; set; }
        public List<ThumbnailModel> thumbnails { get; set; }
        public double regularPrice { get; set; }
        public double oldPrice { get; set; }
        public double retailPrice { get; set; }
        public string content { get; set; }
    }
}
