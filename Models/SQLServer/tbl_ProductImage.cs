using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ANNwpsync.Models.SQLServer
{
    public class tbl_ProductImage
    {
        public int ID { get; set; }
        public Nullable<int> ProductID { get; set; }
        public string ProductImage { get; set; }
        public Nullable<bool> IsHidden { get; set; }
        public Nullable<System.DateTime> CreatedDate { get; set; }
        public string CreatedBy { get; set; }
        public Nullable<System.DateTime> ModifiedDate { get; set; }
        public string ModifiedBy { get; set; }
    }
}
