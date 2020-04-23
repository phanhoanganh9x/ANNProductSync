using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ANNwpsync.Models.SQLServer
{
    public class ProductTag
    {
        public int ID { get; set; }
        public int TagID { get; set; }
        public int ProductID { get; set; }
        public int ProductVariableID { get; set; }
        public string SKU { get; set; }
        public int CreatedBy { get; set; }
        public System.DateTime CreatedDate { get; set; }
    }
}
