using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ANNwpsync.Models
{
    public class ProductFilterModel
    {
        public string categorySlug { get; set; }
        public List<string> categorySlugList { get; set; }
        public string productBadge { get; set; }
        public string productSearch { get; set; }
        public int productSort { get; set; }
        public string tagSlug { get; set; }
        public int priceMin { get; set; }
        public int priceMax { get; set; }
    }
}
