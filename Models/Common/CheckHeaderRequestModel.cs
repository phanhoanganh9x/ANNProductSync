using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ANNProductSync.Models
{
    public class CheckHeaderRequestModel
    {
        public bool status { get; set; }
        public string message { get; set; }
        public WooCommerce wc { get; set; }
        public string priceType { get; set; }
    }
}
