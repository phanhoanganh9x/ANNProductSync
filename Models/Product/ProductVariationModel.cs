using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ANNwpsync.Models
{
    public class ProductVariationModel
    {
        public string sku { get; set; }
        public int regular_price { get; set; }
        public int retail_price { get; set; }
        public int stock_quantity { get; set; }
        public string image { get; set; }
        public string color { get; set; }
        public string size { get; set; }
    }
}
