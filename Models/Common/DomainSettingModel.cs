using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace ANNwpsync.Models
{
    public class DomainSettingModel
    {
        public string woocommerce_key { get; set; }
        public string woocommerce_secret { get; set; }
        public string woocommerce_price_type { get; set; }
        public string wordpress_key { get; set; }
        public string wordpress_secret { get; set; }
        public string wordpress_oauth_token { get; set; }
        public string wordpress_oauth_token_secret { get; set; }
        public string username { get; set; }
        public string password { get; set; }
    }
}
