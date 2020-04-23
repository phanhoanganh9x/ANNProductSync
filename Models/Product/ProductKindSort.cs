using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ANNwpsync.Models
{
    public enum ProductSortKind
    {
        PriceAsc = 1,    // Gia tang dan
        PriceDesc = 2,    // Gia giam dan
        ModelNew = 3,    // Kieu moi nhat
        ProductNew = 4     // Hang moi ve
    }
}
