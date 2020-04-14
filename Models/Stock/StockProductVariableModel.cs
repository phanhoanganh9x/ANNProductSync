using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ANNProductSync.Models
{
    public class StockProductVariableModel
    {
        public int productID { get; set; }
        public int productVariableID { get; set; }
        public string sku { get; set; }
        public int quantity { get; set; } = 0;
        public bool availability { get; set; } = false;
    }
}
